import PropTypes from 'prop-types';
import React from 'react';
import { Redirect, Route } from 'react-router-dom';
import BlocklistConnector from 'Activity/Blocklist/BlocklistConnector';
import HistoryConnector from 'Activity/History/HistoryConnector';
import QueueConnector from 'Activity/Queue/QueueConnector';
import AlbumDetailsPageConnector from 'Album/Details/AlbumDetailsPageConnector';
import ArtistDetailsPageConnector from 'Artist/Details/ArtistDetailsPageConnector';
import ArtistIndex from 'Artist/Index/ArtistIndex';
import CalendarPageConnector from 'Calendar/CalendarPageConnector';
import NotFound from 'Components/NotFound';
import Switch from 'Components/Router/Switch';
import AddNewItemConnector from 'Search/AddNewItemConnector';
import CustomFormatSettingsPage from 'Settings/CustomFormats/CustomFormatSettingsPage';
import DownloadClientSettingsConnector from 'Settings/DownloadClients/DownloadClientSettingsConnector';
import GeneralSettingsConnector from 'Settings/General/GeneralSettingsConnector';
import ImportListSettingsConnector from 'Settings/ImportLists/ImportListSettingsConnector';
import IndexerSettingsConnector from 'Settings/Indexers/IndexerSettingsConnector';
import MediaManagementConnector from 'Settings/MediaManagement/MediaManagementConnector';
import MetadataSettings from 'Settings/Metadata/MetadataSettings';
import NotificationSettings from 'Settings/Notifications/NotificationSettings';
import Profiles from 'Settings/Profiles/Profiles';
import QualityConnector from 'Settings/Quality/QualityConnector';
import Settings from 'Settings/Settings';
import TagSettings from 'Settings/Tags/TagSettings';
import UISettingsConnector from 'Settings/UI/UISettingsConnector';
import BackupsConnector from 'System/Backup/BackupsConnector';
import LogsTableConnector from 'System/Events/LogsTableConnector';
import Logs from 'System/Logs/Logs';
import Status from 'System/Status/Status';
import Tasks from 'System/Tasks/Tasks';
import Updates from 'System/Updates/Updates';
import UnmappedFilesTableConnector from 'UnmappedFiles/UnmappedFilesTableConnector';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import CutoffUnmetConnector from 'Wanted/CutoffUnmet/CutoffUnmetConnector';
import MissingConnector from 'Wanted/Missing/MissingConnector';

function AppRoutes(props) {
  const {
    app
  } = props;

  return (
    <Switch>
      {/*
        Artist
      */}

      <Route
        exact={true}
        path="/"
        component={ArtistIndex}
      />

      {
        window.Lidarr.urlBase &&
          <Route
            exact={true}
            path="/"
            addUrlBase={false}
            render={() => {
              return (
                <Redirect
                  to={getPathWithUrlBase('/')}
                  component={app}
                />
              );
            }}
          />
      }

      <Route
        path="/add/search"
        component={AddNewItemConnector}
      />

      <Route
        path="/artisteditor"
        exact={true}
        render={() => {
          return (
            <Redirect
              to={getPathWithUrlBase('/')}
              component={app}
            />
          );
        }}
      />

      <Route
        path="/albumstudio"
        exact={true}
        render={() => {
          return (
            <Redirect
              to={getPathWithUrlBase('/')}
              component={app}
            />
          );
        }}
      />

      <Route
        path="/unmapped"
        component={UnmappedFilesTableConnector}
      />

      <Route
        path="/artist/:foreignArtistId"
        component={ArtistDetailsPageConnector}
      />

      <Route
        path="/album/:foreignAlbumId"
        component={AlbumDetailsPageConnector}
      />

      {/*
        Calendar
      */}

      <Route
        path="/calendar"
        component={CalendarPageConnector}
      />

      {/*
        Activity
      */}

      <Route
        path="/activity/history"
        component={HistoryConnector}
      />

      <Route
        path="/activity/queue"
        component={QueueConnector}
      />

      <Route
        path="/activity/blocklist"
        component={BlocklistConnector}
      />

      {/*
        Wanted
      */}

      <Route
        path="/wanted/missing"
        component={MissingConnector}
      />

      <Route
        path="/wanted/cutoffunmet"
        component={CutoffUnmetConnector}
      />

      {/*
        Settings
      */}

      <Route
        exact={true}
        path="/settings"
        component={Settings}
      />

      <Route
        path="/settings/mediamanagement"
        component={MediaManagementConnector}
      />

      <Route
        path="/settings/profiles"
        component={Profiles}
      />

      <Route
        path="/settings/quality"
        component={QualityConnector}
      />

      <Route
        path="/settings/customformats"
        component={CustomFormatSettingsPage}
      />

      <Route
        path="/settings/indexers"
        component={IndexerSettingsConnector}
      />

      <Route
        path="/settings/downloadclients"
        component={DownloadClientSettingsConnector}
      />

      <Route
        path="/settings/importlists"
        component={ImportListSettingsConnector}
      />

      <Route
        path="/settings/connect"
        component={NotificationSettings}
      />

      <Route
        path="/settings/metadata"
        component={MetadataSettings}
      />

      <Route
        path="/settings/tags"
        component={TagSettings}
      />

      <Route
        path="/settings/general"
        component={GeneralSettingsConnector}
      />

      <Route
        path="/settings/ui"
        component={UISettingsConnector}
      />

      {/*
        System
      */}

      <Route
        path="/system/status"
        component={Status}
      />

      <Route
        path="/system/tasks"
        component={Tasks}
      />

      <Route
        path="/system/backup"
        component={BackupsConnector}
      />

      <Route
        path="/system/updates"
        component={Updates}
      />

      <Route
        path="/system/events"
        component={LogsTableConnector}
      />

      <Route
        path="/system/logs/files"
        component={Logs}
      />

      {/*
        Not Found
      */}

      <Route
        path="*"
        component={NotFound}
      />

    </Switch>
  );
}

AppRoutes.propTypes = {
  app: PropTypes.func.isRequired
};

export default AppRoutes;
