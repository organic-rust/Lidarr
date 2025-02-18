using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class GetArtistPathFixture : CoreTest<XbmcService>
    {
        private const string MB_ID = "9f4e41c3-2648-428e-b8c7-dc10465b49ac";
        private XbmcSettings _settings;
        private Music.Artist _artist;
        private List<KodiArtist> _xbmcArtist;
        private List<KodiSource> _xbmcSources;

        [SetUp]
        public void Setup()
        {
            _settings = Builder<XbmcSettings>.CreateNew()
                                             .Build();

            _xbmcArtist = Builder<KodiArtist>.CreateListOfSize(3)
                                         .All()
                                         .With(s => s.MusicbrainzArtistId = new List<string> { "0" })
                                         .With(s => s.SourceId = new List<int> { 1 })
                                         .TheFirst(1)
                                         .With(s => s.MusicbrainzArtistId = new List<string> { MB_ID.ToString() })
                                         .Build()
                                         .ToList();

            _xbmcSources = Builder<KodiSource>.CreateListOfSize(1)
                             .All()
                             .With(s => s.SourceId = _xbmcArtist.First().SourceId.First())
                             .Build()
                             .ToList();

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetArtist(_settings))
                  .Returns(_xbmcArtist);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetSources(_settings))
                  .Returns(_xbmcSources);
        }

        private void GivenMatchingMusicbrainzId()
        {
            _artist = new Artist
            {
                ForeignArtistId = MB_ID,
                Name = "Artist"
            };
        }

        private void GivenMatchingTitle()
        {
            _artist = new Artist
            {
                ForeignArtistId = "1000",
                Name = _xbmcArtist.First().Label
            };
        }

        private void GivenMatchingArtist()
        {
            _artist = new Artist
            {
                ForeignArtistId = "1000",
                Name = "Does not exist"
            };
        }

        [Test]
        public void should_return_null_when_artist_is_not_found()
        {
            GivenMatchingArtist();

            Subject.GetArtistPath(_settings, _artist).Should().BeNull();
        }

        [Test]
        public void should_return_path_when_musicbrainzId_matches()
        {
            GivenMatchingMusicbrainzId();

            Subject.GetArtistPath(_settings, _artist).Should().Be(_xbmcSources.First().File);
        }

        [Test]
        public void should_return_path_when_title_matches()
        {
            GivenMatchingTitle();

            Subject.GetArtistPath(_settings, _artist).Should().Be(_xbmcSources.First().File);
        }
    }
}
