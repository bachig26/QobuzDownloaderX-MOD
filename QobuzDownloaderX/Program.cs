using Bluegrams.Application;
using QobuzApiSharp.Models.Content;
using QobuzDownloaderX.Properties;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.Shared.Tools;
using QobuzDownloaderX.View;
using System.Globalization;

namespace QobuzDownloaderX
{
    internal static class Program
    {
		const string USER_ID = "...";
        const string AUTH_TOKEN = "...";
		
        private static void Main()
        {
            // Make the default settings class portable
            PortableSettingsProviderBase.SettingsDirectory = FileTools.GetInitializedSettingsDir();
            // Global override of every settings "Roaming" property.
            PortableSettingsProviderBase.AllRoaming = true;
            PortableJsonSettingsProvider.ApplyProvider(Settings.Default);

            // Use en-US formatting everywhere for consistency
            var culture = CultureInfo.GetCultureInfo("en-US");

            //Culture for any thread
            CultureInfo.DefaultThreadCurrentCulture = culture;

            //Culture for UI in any thread
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Create logging dir and clean older logs if present
            Globals.LoggingDir = FileTools.GetInitializedLogDir();

            // Initialise forms
            Globals.LoginForm = new LoginForm();
            Globals.AboutForm = new AboutForm();

            // Initialize Global Tagging options. Selected ArtSize is automatically set in artSizeSelect change event listener.
            Globals.TaggingOptions = new TaggingOptions()
            {
                WriteAlbumNameTag = Settings.Default.albumTag,
                WriteAlbumArtistTag = Settings.Default.albumArtistTag,
                WriteTrackArtistTag = Settings.Default.artistTag,
                WriteCommentTag = Settings.Default.commentTag,
                CommentTag = Settings.Default.commentText,
                WriteComposerTag = Settings.Default.composerTag,
                WriteProducerTag = Settings.Default.producerTag,
                WriteLabelTag = Settings.Default.labelTag,
                WriteInvolvedPeopleTag = Settings.Default.involvedPeopleTag,
                MergePerformers = Settings.Default.mergePerformers,
                PrimaryListSeparator = Settings.Default.initialListSeparator,
                ListEndSeparator = Settings.Default.listEndSeparator,
                WriteCopyrightTag = Settings.Default.copyrightTag,
                WriteDiskNumberTag = Settings.Default.discTag,
                WriteDiskTotalTag = Settings.Default.totalDiscsTag,
                WriteGenreTag = Settings.Default.genreTag,
                WriteIsrcTag = Settings.Default.isrcTag,
                WriteMediaTypeTag = Settings.Default.typeTag,
                WriteExplicitTag = Settings.Default.explicitTag,
                WriteTrackTitleTag = Settings.Default.trackTitleTag,
                WriteTrackNumberTag = Settings.Default.trackTag,
                WriteTrackTotalTag = Settings.Default.totalTracksTag,
                WriteUpcTag = Settings.Default.upcTag,
                WriteReleaseYearTag = Settings.Default.yearTag,
                WriteReleaseDateTag = Settings.Default.releaseDateTag,
                WriteCoverImageTag = Settings.Default.imageTag,
                WriteUrlTag = Settings.Default.urlTag
            };

            var logger = new DownloadLogger(null, () => { });
            // Remove previous download error log
            logger.RemovePreviousErrorLog();

            var download_manager = new DownloadManager(logger, e => { }, e => { });

            QobuzApiServiceManager.Initialize();
            QobuzApiServiceManager.GetApiService().LoginWithToken(USER_ID, AUTH_TOKEN);

            Settings.Default.savedFolder = "D:\\Users\\facade\\Music\\song-dl";
            Globals.FormatIdString = "6";
            Globals.FileNameTemplateString = " - ";
            Globals.MaxLength = 36;
            Globals.AudioFileType = ".flac";

            SearchResult tracksResult = QobuzApiServiceManager.GetApiService().SearchTracks("that look in your eyes g jones", 15, 0, true);
            download_manager.StartDownloadItemTaskAsync(
                DownloadUrlParser.ParseDownloadUrl($"{Globals.WEBPLAYER_BASE_URL}/track/{tracksResult.Tracks.Items[0].Id}"),
                () => { },
                () => { }
            ).Wait();
        }
    }
}