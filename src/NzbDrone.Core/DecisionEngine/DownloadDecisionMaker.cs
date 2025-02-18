using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDecisionEngineSpecification> _specifications;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IParsingService _parsingService;
        private readonly IRemoteAlbumAggregationService _aggregationService;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDecisionEngineSpecification> specifications,
            IParsingService parsingService,
            ICustomFormatCalculationService formatService,
            IRemoteAlbumAggregationService aggregationService,
            Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _formatCalculator = formatService;
            _aggregationService = aggregationService;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false)
        {
            return GetAlbumDecisions(reports).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            return GetAlbumDecisions(reports, false, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetAlbumDecisions(List<ReleaseInfo> reports, bool pushedRelease = false, SearchCriteriaBase searchCriteria = null)
        {
            if (reports.Any())
            {
                _logger.ProgressInfo("Processing {0} releases", reports.Count);
            }
            else
            {
                _logger.ProgressInfo("No results found");
            }

            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);
                _logger.Debug("Processing release '{0}' from '{1}'", report.Title, report.Indexer);

                try
                {
                    var parsedAlbumInfo = Parser.Parser.ParseAlbumTitle(report.Title);

                    if (parsedAlbumInfo == null && searchCriteria != null)
                    {
                        parsedAlbumInfo = Parser.Parser.ParseAlbumTitleWithSearchCriteria(report.Title,
                            searchCriteria.Artist,
                            searchCriteria.Albums);
                    }

                    if (parsedAlbumInfo != null && !parsedAlbumInfo.ArtistName.IsNullOrWhiteSpace())
                    {
                        var remoteAlbum = _parsingService.Map(parsedAlbumInfo, searchCriteria);
                        remoteAlbum.Release = report;

                        _aggregationService.Augment(remoteAlbum);

                        // try parsing again using the search criteria, in case it parsed but parsed incorrectly
                        if ((remoteAlbum.Artist == null || remoteAlbum.Albums.Empty()) && searchCriteria != null)
                        {
                            _logger.Debug("Artist/Album null for {0}, reparsing with search criteria", report.Title);
                            var parsedAlbumInfoWithCriteria = Parser.Parser.ParseAlbumTitleWithSearchCriteria(report.Title,
                                                                                                                searchCriteria.Artist,
                                                                                                                searchCriteria.Albums);

                            if (parsedAlbumInfoWithCriteria != null && parsedAlbumInfoWithCriteria.ArtistName.IsNotNullOrWhiteSpace())
                            {
                                remoteAlbum = _parsingService.Map(parsedAlbumInfoWithCriteria, searchCriteria);
                            }
                        }

                        remoteAlbum.Release = report;

                        if (remoteAlbum.Artist == null)
                        {
                            decision = new DownloadDecision(remoteAlbum, new Rejection("Unknown Artist"));

                            // shove in the searched artist in case of forced download in interactive search
                            if (searchCriteria != null)
                            {
                                remoteAlbum.Artist = searchCriteria.Artist;
                                remoteAlbum.Albums = searchCriteria.Albums;
                            }
                        }
                        else if (remoteAlbum.Albums.Empty())
                        {
                            decision = new DownloadDecision(remoteAlbum, new Rejection("Unable to parse albums from release name"));
                            if (searchCriteria != null)
                            {
                                remoteAlbum.Albums = searchCriteria.Albums;
                            }
                        }
                        else
                        {
                            _aggregationService.Augment(remoteAlbum);

                            remoteAlbum.CustomFormats = _formatCalculator.ParseCustomFormat(remoteAlbum, remoteAlbum.Release.Size);
                            remoteAlbum.CustomFormatScore = remoteAlbum?.Artist?.QualityProfile?.Value.CalculateCustomFormatScore(remoteAlbum.CustomFormats) ?? 0;

                            remoteAlbum.DownloadAllowed = remoteAlbum.Albums.Any();
                            decision = GetDecisionForReport(remoteAlbum, searchCriteria);
                        }
                    }

                    if (searchCriteria != null)
                    {
                        if (parsedAlbumInfo == null)
                        {
                            parsedAlbumInfo = new ParsedAlbumInfo
                            {
                                Quality = QualityParser.ParseQuality(report.Title, null, 0)
                            };
                        }

                        if (parsedAlbumInfo.ArtistName.IsNullOrWhiteSpace())
                        {
                            var remoteAlbum = new RemoteAlbum
                            {
                                Release = report,
                                ParsedAlbumInfo = parsedAlbumInfo
                            };

                            decision = new DownloadDecision(remoteAlbum, new Rejection("Unable to parse release"));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't process release.");

                    var remoteAlbum = new RemoteAlbum { Release = report };
                    decision = new DownloadDecision(remoteAlbum, new Rejection("Unexpected error processing release"));
                }

                reportNumber++;

                if (decision != null)
                {
                    var source = pushedRelease ? ReleaseSourceType.ReleasePush : ReleaseSourceType.Rss;

                    if (searchCriteria != null)
                    {
                        if (searchCriteria.InteractiveSearch)
                        {
                            source = ReleaseSourceType.InteractiveSearch;
                        }
                        else if (searchCriteria.UserInvokedSearch)
                        {
                            source = ReleaseSourceType.UserInvokedSearch;
                        }
                        else
                        {
                            source = ReleaseSourceType.Search;
                        }
                    }

                    decision.RemoteAlbum.ReleaseSource = source;

                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
                    }
                    else
                    {
                        _logger.Debug("Release accepted");
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision GetDecisionForReport(RemoteAlbum remoteAlbum, SearchCriteriaBase searchCriteria = null)
        {
            var reasons = Array.Empty<Rejection>();

            foreach (var specifications in _specifications.GroupBy(v => v.Priority).OrderBy(v => v.Key))
            {
                reasons = specifications.Select(c => EvaluateSpec(c, remoteAlbum, searchCriteria))
                                                        .Where(c => c != null)
                                                        .ToArray();

                if (reasons.Any())
                {
                    break;
                }
            }

            return new DownloadDecision(remoteAlbum, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IDecisionEngineSpecification spec, RemoteAlbum remoteAlbum, SearchCriteriaBase searchCriteriaBase = null)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteAlbum, searchCriteriaBase);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason, spec.Type);
                }
            }
            catch (NotImplementedException)
            {
                _logger.Trace("Spec " + spec.GetType().Name + " not implemented.");
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteAlbum.Release.ToJson());
                e.Data.Add("parsed", remoteAlbum.ParsedAlbumInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}", remoteAlbum.Release.Title);
                return new Rejection($"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }
    }
}
