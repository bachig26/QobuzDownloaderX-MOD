using QobuzDownloaderX.Models.Download;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.Shared.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QobuzDownloaderX.View
{
    public partial class QobuzDownloaderX : HeadlessForm
    {
        private readonly DownloadLogger logger;

        public readonly DownloadManager DownloadManager;
        public readonly Queue<string> DownloadQueue = new Queue<string>();

        public QobuzDownloaderX()
        {
            InitializeComponent();

            logger = new DownloadLogger(output, UpdateControlsDownloadEnd);
            // Remove previous download error log
            logger.RemovePreviousErrorLog();

            DownloadManager = new DownloadManager(logger, UpdateAlbumTagsUI, UpdateDownloadSpeedLabel)
            {
                CheckIfStreamable = streamableCheckbox.Checked
            };
        }

        public string DownloadLogPath { get; set; }

        public int DevClickEggThingValue { get; set; }
        public int DebugMode { get; set; }

        // Button color download inactive
        private readonly Color ReadyButtonBackColor = Color.FromArgb(0, 112, 239); // Windows Blue (Azure Blue)

        // Button color download active
        private readonly Color busyButtonBackColor = Color.FromArgb(200, 30, 0); // Red

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Set main form size on launch and bring to top-left corner.
            this.Height = 533;
            // this.StartPosition = FormStartPosition.Manual;
			// this.Location = new Point(0, 0);

            // Grab profile image
            var profilePic = Convert.ToString(Globals.Login.User.Avatar);
            profilePictureBox.ImageLocation = profilePic.Replace(@"\", null).Replace("s=50", "s=20");

            // Welcome the user after successful login.
            logger.ClearUiLogComponent();
            output.Invoke(new Action(() => output.AppendText("Welcome " + Globals.Login.User.DisplayName + " (" + Globals.Login.User.Email + ") !\r\n")));
            output.Invoke(new Action(() => output.AppendText("User Zone - " + Globals.Login.User.Zone + "\r\n\r\n")));
            output.Invoke(new Action(() => output.AppendText("Qobuz Credential Description - " + Globals.Login.User.Credential.Description + "\r\n")));
            output.Invoke(new Action(() => output.AppendText("\r\n")));
            output.Invoke(new Action(() => output.AppendText("Qobuz Subscription Details\r\n")));
            output.Invoke(new Action(() => output.AppendText("==========================\r\n")));

            if (Globals.Login.User.Subscription != null)
            {
                output.Invoke(new Action(() => output.AppendText("Offer Type - " + Globals.Login.User.Subscription.Offer + "\r\n")));
                output.Invoke(new Action(() => output.AppendText("Start Date - ")));
                output.Invoke(new Action(() => output.AppendText(Globals.Login.User.Subscription.StartDate != null ? ((DateTimeOffset)Globals.Login.User.Subscription.StartDate).ToString("dd-MM-yyyy") : "?")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("End Date - ")));
                output.Invoke(new Action(() => output.AppendText(Globals.Login.User.Subscription.StartDate != null ? ((DateTimeOffset)Globals.Login.User.Subscription.EndDate).ToString("dd-MM-yyyy") : "?")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("Periodicity - " + Globals.Login.User.Subscription.Periodicity + "\r\n")));
                output.Invoke(new Action(() => output.AppendText("==========================\r\n\r\n")));
            }
            else if (Globals.Login.User.Credential.Parameters.Source == "household" && Globals.Login.User.Credential.Parameters.HiresStreaming == true)
            {
                output.Invoke(new Action(() => output.AppendText("Active Family sub-account, unknown End Date \r\n")));
                output.Invoke(new Action(() => output.AppendText("Credential Label - " + Globals.Login.User.Credential.Label + "\r\n")));
                output.Invoke(new Action(() => output.AppendText("==========================\r\n\r\n")));
            }
            else
            {
                output.Invoke(new Action(() => output.AppendText("No active subscriptions, only sample downloads possible!\r\n")));
                output.Invoke(new Action(() => output.AppendText("==========================\r\n\r\n")));
            }

            output.Invoke(new Action(() => output.AppendText("Your user_auth_token has been set for this session!")));

            // Get and display version number.
            verNumLabel.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Set a placeholder image for Cover Art box.
            albumArtPicBox.ImageLocation = Globals.DEFAULT_COVER_ART_URL;

            // Change account info for logout button
            var oldText = logoutLabel.Text;
            logoutLabel.Text = oldText.Replace("%name%", Globals.Login.User.DisplayName);

            // Initialize Global Tagging options. Selected ArtSize is automatically set in artSizeSelect change event listener.
            Globals.TaggingOptions = new TaggingOptions
            {
                WriteAlbumNameTag = Properties.Settings.Default.albumTag,
                WriteAlbumArtistTag = Properties.Settings.Default.albumArtistTag,
                WriteTrackArtistTag = Properties.Settings.Default.artistTag,
                WriteCommentTag = Properties.Settings.Default.commentTag,
                CommentTag = Properties.Settings.Default.commentText,
                WriteComposerTag = Properties.Settings.Default.composerTag,
                WriteProducerTag = Properties.Settings.Default.producerTag,
                WriteLabelTag = Properties.Settings.Default.labelTag,
                WriteInvolvedPeopleTag = Properties.Settings.Default.involvedPeopleTag,
                MergePerformers = Properties.Settings.Default.mergePerformers,
                PrimaryListSeparator = Properties.Settings.Default.initialListSeparator,
                ListEndSeparator = Properties.Settings.Default.listEndSeparator,
                WriteCopyrightTag = Properties.Settings.Default.copyrightTag,
                WriteDiskNumberTag = Properties.Settings.Default.discTag,
                WriteDiskTotalTag = Properties.Settings.Default.totalDiscsTag,
                WriteGenreTag = Properties.Settings.Default.genreTag,
                WriteIsrcTag = Properties.Settings.Default.isrcTag,
                WriteMediaTypeTag = Properties.Settings.Default.typeTag,
                WriteExplicitTag = Properties.Settings.Default.explicitTag,
                WriteTrackTitleTag = Properties.Settings.Default.trackTitleTag,
                WriteTrackNumberTag = Properties.Settings.Default.trackTag,
                WriteTrackTotalTag = Properties.Settings.Default.totalTracksTag,
                WriteUpcTag = Properties.Settings.Default.upcTag,
                WriteReleaseYearTag = Properties.Settings.Default.yearTag,
                WriteReleaseDateTag = Properties.Settings.Default.releaseDateTag,
                WriteCoverImageTag = Properties.Settings.Default.imageTag,
                WriteUrlTag = Properties.Settings.Default.urlTag
            };

            // Set saved settings to correct places.
            folderBrowserDialog.SelectedPath = Properties.Settings.Default.savedFolder;
            albumCheckbox.Checked = Properties.Settings.Default.albumTag;
            albumArtistCheckbox.Checked = Properties.Settings.Default.albumArtistTag;
            artistCheckbox.Checked = Properties.Settings.Default.artistTag;
            commentCheckbox.Checked = Properties.Settings.Default.commentTag;
            commentTextbox.Text = Properties.Settings.Default.commentText;
            composerCheckbox.Checked = Properties.Settings.Default.composerTag;
            producerCheckbox.Checked = Properties.Settings.Default.producerTag;
            labelCheckbox.Checked = Properties.Settings.Default.labelTag;
            involvedPeopleCheckBox.Checked = Properties.Settings.Default.involvedPeopleTag;
            mergePerformersCheckBox.Checked = Properties.Settings.Default.mergePerformers;
            copyrightCheckbox.Checked = Properties.Settings.Default.copyrightTag;
            discNumberCheckbox.Checked = Properties.Settings.Default.discTag;
            discTotalCheckbox.Checked = Properties.Settings.Default.totalDiscsTag;
            genreCheckbox.Checked = Properties.Settings.Default.genreTag;
            isrcCheckbox.Checked = Properties.Settings.Default.isrcTag;
            typeCheckbox.Checked = Properties.Settings.Default.typeTag;
            explicitCheckbox.Checked = Properties.Settings.Default.explicitTag;
            trackTitleCheckbox.Checked = Properties.Settings.Default.trackTitleTag;
            trackNumberCheckbox.Checked = Properties.Settings.Default.trackTag;
            trackTotalCheckbox.Checked = Properties.Settings.Default.totalTracksTag;
            upcCheckbox.Checked = Properties.Settings.Default.upcTag;
            releasYearCheckbox.Checked = Properties.Settings.Default.yearTag;
            releaseDateCheckbox.Checked = Properties.Settings.Default.releaseDateTag;
            imageCheckbox.Checked = Properties.Settings.Default.imageTag;
            urlCheckBox.Checked = Properties.Settings.Default.urlTag;
            mp3Checkbox.Checked = Properties.Settings.Default.quality1;
            flacLowCheckbox.Checked = Properties.Settings.Default.quality2;
            flacMidCheckbox.Checked = Properties.Settings.Default.quality3;
            flacHighCheckbox.Checked = Properties.Settings.Default.quality4;
            Globals.FormatIdString = Properties.Settings.Default.qualityFormat;
            Globals.AudioFileType = Properties.Settings.Default.audioType;
            artSizeSelect.SelectedIndex = Properties.Settings.Default.savedArtSize;
            filenameTempSelect.SelectedIndex = Properties.Settings.Default.savedFilenameTemplate;
            Globals.FileNameTemplateString = Properties.Settings.Default.savedFilenameTemplateString;
            Globals.MaxLength = Properties.Settings.Default.savedMaxLength;

            customFormatIDTextbox.Text = Globals.FormatIdString;
            maxLengthTextbox.Text = Globals.MaxLength.ToString();
            InitialListSeparatorTextbox.Text = Properties.Settings.Default.initialListSeparator;
            ListEndSeparatorTextbox.Text = Properties.Settings.Default.listEndSeparator;

            // Check if there's no selected path saved.
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                // If there is NOT a saved path.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("No default path has been set! Remember to Choose a Folder!\r\n")));
            }
            else
            {
                // If there is a saved path.
                output.Invoke(new Action(() => output.AppendText("\r\n\r\n")));
                output.Invoke(new Action(() => output.AppendText("Using the last folder you've selected as your selected path!\r\n")));
                output.Invoke(new Action(() => output.AppendText("\r\n")));
                output.Invoke(new Action(() => output.AppendText("Default Folder:\r\n")));
                output.Invoke(new Action(() => output.AppendText(folderBrowserDialog.SelectedPath + "\r\n")));
            }

            // Run anything put into the debug events (For Testing)
            debuggingEvents(sender, e);

            UpdateQueueLabel();
        }

        public void UpdateDownloadSpeedLabel(string speed)
        {
            downloadSpeedLabel.Invoke(new Action(() => downloadSpeedLabel.Text = speed));
        }

        private void debuggingEvents(object sender, EventArgs e)
        {
            DevClickEggThingValue = 0;

            // Debug mode for things that are only for testing, or shouldn't be on public releases. At the moment, does nothing.
            DebugMode = !Debugger.IsAttached ? 0 : 1;

            // Show app_secret value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\napp_secret = " + Globals.AppSecret)));

            // Show format_id value.
            //output.Invoke(new Action(() => output.AppendText("\r\n\r\nformat_id = " + Globals.FormatIdString)));
        }

        private void OpenSearch_Click(object sender, EventArgs e)
        {
            Globals.SearchForm.ShowDialog(this);
        }

        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            if (!DownloadManager.IsBusy)
            {
                await StartLinkItemDownloadAsync(downloadUrl.Text);
            }
            else
            {
                DownloadManager.StopDownloadTask();
            }
        }

        private async void DownloadUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;

            await StartLinkItemDownloadAsync(downloadUrl.Text);
        }

        public async Task StartLinkItemDownloadAsync(string downloadLink)
        {
            // Check if there's no selected path.
            if (string.IsNullOrEmpty(Properties.Settings.Default.savedFolder))
            {
                // If there is NOT a saved path.
                logger.ClearUiLogComponent();
                output.Invoke(new Action(() => output.AppendText($"No path has been set! Remember to Choose a Folder!{Environment.NewLine}")));
                return;
            }

            // Get download item type and ID from url
            var downloadItem = DownloadUrlParser.ParseDownloadUrl(downloadLink);

            // If download item could not be parsed, abort
            if (downloadItem.IsEmpty())
            {
                logger.ClearUiLogComponent();
                output.Invoke(new Action(() => output.AppendText("URL not understood. Is there a typo?")));
                return;
            }

            // If, for some reason, a download is still busy, do nothing
            if (DownloadManager.IsBusy)
            {
                return;
            }

            // Run the StartDownloadItemTaskAsync method on a background thread & Wait for the task to complete
            await Task.Run(() => DownloadManager.StartDownloadItemTaskAsync(downloadItem, UpdateControlsDownloadStart, UpdateControlsDownloadEnd));

            if (DownloadQueue.Count > 0)
            {
#pragma warning disable CS4014
                Task.Run(() => StartLinkItemDownloadAsync(DownloadQueue.Dequeue())).ConfigureAwait(false);
#pragma warning restore CS4014
                UpdateQueueLabel();
            }
        }

        public void UpdateQueueLabel()
        {
            queueLabel.Invoke(new Action(() => queueLabel.Text = DownloadQueue.Count > 0
                ? $"{DownloadQueue.Count} download{(DownloadQueue.Count > 1 ? "s" : "")} in queue"
                : ""));
        }

        public void UpdateControlsDownloadStart()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.AutoCheck = false));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.AutoCheck = false));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.AutoCheck = false));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.AutoCheck = false));

            downloadUrl.Invoke(new Action(() => downloadUrl.Enabled = false));

            selectFolderButton.Invoke(new Action(() => selectFolderButton.Enabled = false));

            // openSearchButton.Invoke(new Action(() => openSearchButton.Enabled = false));

            downloadButton.Invoke(new Action(() =>
            {
                downloadButton.Text = "Stop Download";
                downloadButton.BackColor = busyButtonBackColor;
            }));
        }

        public void UpdateControlsDownloadEnd()
        {
            mp3Checkbox.Invoke(new Action(() => mp3Checkbox.AutoCheck = true));
            flacLowCheckbox.Invoke(new Action(() => flacLowCheckbox.AutoCheck = true));
            flacMidCheckbox.Invoke(new Action(() => flacMidCheckbox.AutoCheck = true));
            flacHighCheckbox.Invoke(new Action(() => flacHighCheckbox.AutoCheck = true));

            downloadUrl.Invoke(new Action(() => downloadUrl.Enabled = true));

            selectFolderButton.Invoke(new Action(() => selectFolderButton.Enabled = true));
            openSearchButton.Invoke(new Action(() => openSearchButton.Enabled = true));

            downloadButton.Invoke(new Action(() =>
            {
                downloadButton.Text = "Download";
                downloadButton.BackColor = ReadyButtonBackColor;
            }));
        }

        private void SelectFolder_Click(object sender, EventArgs e)
        {
            var t = new Thread(() =>
            {
                // Open Folder Browser to select path & Save the selection
                folderBrowserDialog.ShowDialog();
                Properties.Settings.Default.savedFolder = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.Save();
            });

            // Run your code from a thread that joins the STA Thread
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            // Open selected folder
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                // If there's no selected path.
                MessageBox.Show("No path selected!", "ERROR",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                UpdateControlsDownloadEnd();
            }
            else
            {
                // If selected path doesn't exist, create it. (Will be ignored if it does)
                System.IO.Directory.CreateDirectory(folderBrowserDialog.SelectedPath);
                // Open selected folder
                Process.Start(@folderBrowserDialog.SelectedPath);
            }
        }

        private void OpenLogFolderButton_Click(object sender, EventArgs e)
        {
            // Open log folder. Folder should exist here so no extra check
            Process.Start(@Globals.LoggingDir);
        }

        // Update UI for downloading album
        public void UpdateAlbumTagsUI(DownloadItemInfo downloadInfo)
        {
            //  Display album art
            albumArtPicBox.Invoke(new Action(() => albumArtPicBox.ImageLocation = downloadInfo.FrontCoverImgBoxUrl));

            // Display album quality in Quality textbox.
            qualityTextbox.Invoke(new Action(() => qualityTextbox.Text = downloadInfo.DisplayQuality));

            // Display album info textfields
            albumArtistTextBox.Invoke(new Action(() => albumArtistTextBox.Text = downloadInfo.AlbumArtist));
            albumTextBox.Invoke(new Action(() => albumTextBox.Text = downloadInfo.AlbumName));
            releaseDateTextBox.Invoke(new Action(() => releaseDateTextBox.Text = downloadInfo.ReleaseDate));
            upcTextBox.Invoke(new Action(() => upcTextBox.Text = downloadInfo.Upc));
            totalTracksTextbox.Invoke(new Action(() => totalTracksTextbox.Text = downloadInfo.TrackTotal.ToString()));
        }

        private void tagsLabel_Click(object sender, EventArgs e)
        {
            if (this.Height == 533)
            {
                //New Height
                this.Height = 660;
                tagsLabel.Text = "🠉 Choose which tags to save (click me) 🠉";
            }
            else if (this.Height == 660)
            {
                //New Height
                this.Height = 533;
                tagsLabel.Text = "🠋 Choose which tags to save (click me) 🠋";
            }
        }

        private void AlbumCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.albumTag = albumCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteAlbumNameTag = albumCheckbox.Checked;
        }

        private void AlbumArtistCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.albumArtistTag = albumArtistCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteAlbumArtistTag = albumArtistCheckbox.Checked;
        }

        private void TrackTitleCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.trackTitleTag = trackTitleCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteTrackTitleTag = trackTitleCheckbox.Checked;
        }

        private void ArtistCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.artistTag = artistCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteTrackArtistTag = artistCheckbox.Checked;
        }

        private void TrackNumberCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.trackTag = trackNumberCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteTrackNumberTag = trackNumberCheckbox.Checked;
        }

        private void TrackTotalCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.totalTracksTag = trackTotalCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteTrackTotalTag = trackTotalCheckbox.Checked;
        }

        private void DiscNumberCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.discTag = discNumberCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteDiskNumberTag = discNumberCheckbox.Checked;
        }

        private void DiscTotalCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.totalDiscsTag = discTotalCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteDiskTotalTag = discTotalCheckbox.Checked;
        }

        private void ReleaseYearCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.yearTag = releasYearCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteReleaseYearTag = releasYearCheckbox.Checked;
        }

        private void ReleaseDateCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.releaseDateTag = releaseDateCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteReleaseDateTag = releaseDateCheckbox.Checked;
        }

        private void GenreCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.genreTag = genreCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteGenreTag = genreCheckbox.Checked;
        }

        private void ComposerCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.composerTag = composerCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteComposerTag = composerCheckbox.Checked;
        }

        private void CopyrightCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.copyrightTag = copyrightCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteCopyrightTag = copyrightCheckbox.Checked;
        }

        private void IsrcCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.isrcTag = isrcCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteIsrcTag = isrcCheckbox.Checked;
        }

        private void TypeCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.typeTag = typeCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteMediaTypeTag = typeCheckbox.Checked;
        }

        private void UpcCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.upcTag = upcCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteUpcTag = upcCheckbox.Checked;
        }

        private void ExplicitCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.explicitTag = explicitCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteExplicitTag = explicitCheckbox.Checked;
        }

        private void CommentCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.commentTag = commentCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteCommentTag = commentCheckbox.Checked;
        }

        private void ImageCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.imageTag = imageCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteCoverImageTag = imageCheckbox.Checked;
        }

        private void CommentTextbox_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.commentText = commentTextbox.Text;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.CommentTag = commentTextbox.Text;
        }

        private void ArtSizeSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Set ArtSize to selected value, and save selected option to settings.
            Globals.TaggingOptions.ArtSize = artSizeSelect.Text;
            Properties.Settings.Default.savedArtSize = artSizeSelect.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void filenameTempSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (filenameTempSelect.SelectedIndex)
            {
                // Set filename template to selected value, and save selected option to settings.
                case 0:
                    Globals.FileNameTemplateString = " ";

                    break;

                case 1:
                    Globals.FileNameTemplateString = " - ";

                    break;

                default:
                    Globals.FileNameTemplateString = " ";

                    break;
            }

            Properties.Settings.Default.savedFilenameTemplate = filenameTempSelect.SelectedIndex;
            Properties.Settings.Default.savedFilenameTemplateString = Globals.FileNameTemplateString;
            Properties.Settings.Default.Save();
        }

        private void maxLengthTextbox_TextChanged(object sender, EventArgs e)
        {
            if (maxLengthTextbox.Text != null)
            {
                try
                {
                    if (Convert.ToInt32(maxLengthTextbox.Text) > 150)
                    {
                        maxLengthTextbox.Text = "150";
                    }
                    Properties.Settings.Default.savedMaxLength = Convert.ToInt32(maxLengthTextbox.Text);
                    Properties.Settings.Default.Save();

                    Globals.MaxLength = Convert.ToInt32(maxLengthTextbox.Text);
                }
                catch (Exception)
                {
                    Globals.MaxLength = 100;
                }
            }
            else
            {
                Globals.MaxLength = 100;
            }
        }

        private void ProducerCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.producerTag = producerCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteProducerTag = producerCheckbox.Checked;
        }

        private void LabelCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.labelTag = labelCheckbox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteLabelTag = labelCheckbox.Checked;
        }

        private void InvolvedPeopleCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.involvedPeopleTag = involvedPeopleCheckBox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteInvolvedPeopleTag = involvedPeopleCheckBox.Checked;
        }

        private void MergePerformersCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.mergePerformers = mergePerformersCheckBox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.MergePerformers = mergePerformersCheckBox.Checked;
        }

        private void InitialListSeparatorTextbox_TextChanged(object sender, EventArgs e)
        {
            if (InitialListSeparatorTextbox.Text != null)
            {
                Properties.Settings.Default.initialListSeparator = InitialListSeparatorTextbox.Text;
                Properties.Settings.Default.Save();
                Globals.TaggingOptions.PrimaryListSeparator = InitialListSeparatorTextbox.Text;
            }
            else
            {
                Properties.Settings.Default.initialListSeparator = ", ";
                Properties.Settings.Default.Save();
                Globals.TaggingOptions.PrimaryListSeparator = InitialListSeparatorTextbox.Text;
            }
        }

        private void ListEndSeparatorTextbox_TextChanged(object sender, EventArgs e)
        {
            if (ListEndSeparatorTextbox.Text != null)
            {
                Properties.Settings.Default.listEndSeparator = ListEndSeparatorTextbox.Text;
                Properties.Settings.Default.Save();
                Globals.TaggingOptions.ListEndSeparator = ListEndSeparatorTextbox.Text;
            }
            else
            {
                Properties.Settings.Default.listEndSeparator = " & ";
                Properties.Settings.Default.Save();
                Globals.TaggingOptions.ListEndSeparator = ListEndSeparatorTextbox.Text;
            }
        }

        private void UrlCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.urlTag = urlCheckBox.Checked;
            Properties.Settings.Default.Save();
            Globals.TaggingOptions.WriteUrlTag = urlCheckBox.Checked;
        }

        private void flacHighCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.quality4 = flacHighCheckbox.Checked;
            Properties.Settings.Default.Save();

            if (flacHighCheckbox.Checked)
            {
                Globals.FormatIdString = "27";
                customFormatIDTextbox.Text = "27";
                Globals.AudioFileType = ".flac";
                Properties.Settings.Default.qualityFormat = Globals.FormatIdString;
                Properties.Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacMidCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacMidCheckbox.Checked && !flacLowCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void flacMidCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.quality3 = flacMidCheckbox.Checked;
            Properties.Settings.Default.Save();

            if (flacMidCheckbox.Checked)
            {
                Globals.FormatIdString = "7";
                customFormatIDTextbox.Text = "7";
                Globals.AudioFileType = ".flac";
                Properties.Settings.Default.qualityFormat = Globals.FormatIdString;
                Properties.Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacLowCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void flacLowCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.quality2 = flacLowCheckbox.Checked;
            Properties.Settings.Default.Save();

            if (flacLowCheckbox.Checked)
            {
                Globals.FormatIdString = "6";
                customFormatIDTextbox.Text = "6";
                Globals.AudioFileType = ".flac";
                Properties.Settings.Default.qualityFormat = Globals.FormatIdString;
                Properties.Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacMidCheckbox.Checked = false;
                mp3Checkbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacMidCheckbox.Checked && !mp3Checkbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void mp3Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.quality1 = mp3Checkbox.Checked;
            Properties.Settings.Default.Save();

            if (mp3Checkbox.Checked)
            {
                Globals.FormatIdString = "5";
                customFormatIDTextbox.Text = "5";
                Globals.AudioFileType = ".mp3";
                Properties.Settings.Default.qualityFormat = Globals.FormatIdString;
                Properties.Settings.Default.audioType = Globals.AudioFileType;
                downloadButton.Enabled = true;
                flacHighCheckbox.Checked = false;
                flacMidCheckbox.Checked = false;
                flacLowCheckbox.Checked = false;
            }
            else
            {
                if (!flacHighCheckbox.Checked && !flacMidCheckbox.Checked && !flacLowCheckbox.Checked)
                {
                    downloadButton.Enabled = false;
                }
            }
        }

        private void customFormatIDTextbox_TextChanged(object sender, EventArgs e)
        {
            if (Globals.FormatIdString != "5" || Globals.FormatIdString != "6" || Globals.FormatIdString != "7" || Globals.FormatIdString != "27")
            {
                Globals.FormatIdString = customFormatIDTextbox.Text;
            }
        }

        private void exitLabel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void minimizeLabel_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void minimizeLabel_MouseHover(object sender, EventArgs e)
        {
            minimizeLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void minimizeLabel_MouseLeave(object sender, EventArgs e)
        {
            minimizeLabel.ForeColor = Color.White;
        }

        private void aboutLabel_Click(object sender, EventArgs e)
        {
            Globals.AboutForm.ShowDialog();
        }

        private void aboutLabel_MouseHover(object sender, EventArgs e)
        {
            aboutLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void aboutLabel_MouseLeave(object sender, EventArgs e)
        {
            aboutLabel.ForeColor = Color.White;
        }

        private void exitLabel_MouseHover(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void exitLabel_MouseLeave(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.White;
        }

        private void QobuzDownloaderX_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void QobuzDownloaderX_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void logoBox_Click(object sender, EventArgs e)
        {
            DevClickEggThingValue += 1;

            if (DevClickEggThingValue >= 3)
            {
                streamableCheckbox.Visible = true;
                enableBtnsButton.Visible = true;
                hideDebugButton.Visible = true;
                displaySecretButton.Visible = true;
                secretTextbox.Visible = true;
                hiddenTextPanel.Visible = true;
                customFormatIDTextbox.Visible = true;
                customFormatPanel.Visible = true;
                formatIDLabel.Visible = true;
            }
            else
            {
                streamableCheckbox.Visible = false;
                displaySecretButton.Visible = false;
                secretTextbox.Visible = false;
                hiddenTextPanel.Visible = false;
                enableBtnsButton.Visible = false;
                hideDebugButton.Visible = false;
                customFormatIDTextbox.Visible = false;
                customFormatPanel.Visible = false;
                formatIDLabel.Visible = false;
            }
        }

        private void hideDebugButton_Click(object sender, EventArgs e)
        {
            streamableCheckbox.Visible = false;
            displaySecretButton.Visible = false;
            secretTextbox.Visible = false;
            hiddenTextPanel.Visible = false;
            enableBtnsButton.Visible = false;
            hideDebugButton.Visible = false;
            customFormatIDTextbox.Visible = false;
            customFormatPanel.Visible = false;
            formatIDLabel.Visible = false;

            DevClickEggThingValue = 0;
        }

        private void displaySecretButton_Click(object sender, EventArgs e)
        {
            secretTextbox.Text = QobuzApiServiceManager.GetApiService().AppSecret;
        }

        private void logoutLabel_MouseHover(object sender, EventArgs e)
        {
            logoutLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void logoutLabel_MouseLeave(object sender, EventArgs e)
        {
            logoutLabel.ForeColor = Color.FromArgb(88, 92, 102);
        }

        private void logoutLabel_Click(object sender, EventArgs e)
        {
            // Could use some work, but this works.
            Process.Start("QobuzDownloaderX.exe");
            Application.Exit();
        }

        private void enableBtnsButton_Click(object sender, EventArgs e)
        {
            UpdateControlsDownloadEnd();
        }

        private void StreamableCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            DownloadManager.CheckIfStreamable = streamableCheckbox.Checked;
        }
    }
}