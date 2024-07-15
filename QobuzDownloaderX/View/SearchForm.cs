using QobuzApiSharp.Exceptions;
using QobuzApiSharp.Models.Content;
using QobuzDownloaderX.Models.UI;
using QobuzDownloaderX.Shared;
using QobuzDownloaderX.Shared.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QobuzDownloaderX.View
{
    public partial class SearchForm : HeadlessForm
    {
        private readonly string errorLog = Path.Combine(Globals.LoggingDir, "Search_Errors.log");
        TableLayoutPanel resultsTableLayoutPanel;

        public SearchForm()
        {
            InitializeComponent();

            // Remove previous search error log
            if (File.Exists(errorLog))
            {
                File.Delete(errorLog);
            }

            ControlTools.SetDoubleBuffered(containerScrollPanel);

            searchTypeSelect.SelectedItem = "Album";
        }

        private void ResetResultsTableLayoutPanel()
        {
            if (resultsTableLayoutPanel != null)
            {
                containerScrollPanel.Controls.Clear();
                ControlTools.RemoveControls(resultsTableLayoutPanel);
                resultsTableLayoutPanel.Dispose();
            }

            resultsTableLayoutPanel = new TableLayoutPanel
            {
                Name = "resultsTableLayoutPanel",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(33, 33, 33),
                Location = new Point(0, 0),
                Size = new Size(894, 70),
                TabIndex = 4
            };
            ControlTools.SetDoubleBuffered(resultsTableLayoutPanel);
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 613F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 101F));
            resultsTableLayoutPanel.CellPaint += ResultsTableLayoutPanel_CellPaint;

            containerScrollPanel.Controls.Add(resultsTableLayoutPanel);
        }

        private void ShowAndLogSearchResultError(Exception ex)
        {
            ResetResultsTableLayoutPanel();

            resultsTableLayoutPanel.RowCount++;
            resultsTableLayoutPanel.ColumnStyles.Clear();
            resultsTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 914F));
            resultsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            var errorMessage = CreateTextBox($"{ex.Message}", true, GetResultRowColor(0), Color.OrangeRed, FontManager.CreateFont("Hanken Grotesk Medium", 10, FontStyle.Bold | FontStyle.Italic), BorderStyle.None);
            ResizeControlForText(errorMessage, 5);
            resultsTableLayoutPanel.Controls.Add(errorMessage, 0, 0);

            var errorSavedMessage = CreateTextBox($"Error log saved to {errorLog}.", true, GetResultRowColor(0), Color.OrangeRed, FontManager.CreateFont("Hanken Grotesk Medium", 10, FontStyle.Bold | FontStyle.Italic), BorderStyle.None);
            ResizeControlForText(errorSavedMessage, 5);
            resultsTableLayoutPanel.Controls.Add(errorSavedMessage, 0, 1);

            var errorLines = new List<string> { ex.Message };

            switch (ex)
            {
                case ApiErrorResponseException erEx:
                    errorLines.Add($"Failed API request: {erEx.RequestContent}");
                    errorLines.Add($"Api response code: {erEx.ResponseStatusCode}");
                    errorLines.Add($"Api response status: {erEx.ResponseStatus}");
                    errorLines.Add($"Api response reason: {erEx.ResponseReason}");
                    errorLines.Add("");
                    break;
                case ApiResponseParseErrorException pEx:
                    errorLines.Add($"Api response content: {pEx.ResponseContent}");
                    errorLines.Add("");
                    break;
            }

            errorLines.Add(ex.ToString());

            // Write detailed info to log
            File.AppendAllLines(errorLog, errorLines);
        }

        private void ExitLabel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ExitLabel_MouseHover(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.FromArgb(0, 112, 239);
        }

        private void ExitLabel_MouseLeave(object sender, EventArgs e)
        {
            exitLabel.ForeColor = Color.White;
        }

        private void Fill_AlbumResultsTablePanel(SearchResult searchResult)
        {
            FillResultsTablePanel(searchResult?.Albums?.Items, album => new SearchResultRow
            {
                ThumbnailUrl = album.Image.Thumbnail,
                Artist = album.Artist.Name,
                Title = StringTools.DecodeEncodedNonAsciiCharacters(album.Version != null ? $"{album.Title.TrimEnd()} ({album.Version})" : album.Title.TrimEnd()),
                Explicit = album.ParentalWarning.GetValueOrDefault(),
                FormattedDuration = StringTools.FormatDurationInSeconds(album.Duration.GetValueOrDefault()),
                FormattedQuality = $"{album.MaximumBitDepth}-Bit / {album.MaximumSamplingRate} kHz",
                WebPlayerUrl = $"{Globals.WEBPLAYER_BASE_URL}/album/{album.Id}",
                StoreUrl = album.Url,
                ReleaseDate = album.ReleaseDateOriginal.GetValueOrDefault().ToString("yyyy-MM-dd"),
                HiRes = album.Hires.GetValueOrDefault(),
                TrackCount = album.TracksCount.GetValueOrDefault()
            });
        }

        private void Fill_TrackResultsTablePanel(SearchResult searchResult)
        {
            FillResultsTablePanel(searchResult?.Tracks?.Items, track => new SearchResultRow
            {
                ThumbnailUrl = track.Album.Image.Thumbnail,
                Artist = track.Performer.Name,
                Title = StringTools.DecodeEncodedNonAsciiCharacters(track.Version != null ? $"{track.Title.TrimEnd()} ({track.Version})" : track.Title.TrimEnd()),
                Explicit = track.ParentalWarning.GetValueOrDefault(),
                FormattedDuration = StringTools.FormatDurationInSeconds(track.Duration.GetValueOrDefault()),
                FormattedQuality = $"{track.MaximumBitDepth}-Bit / {track.MaximumSamplingRate} kHz",
                WebPlayerUrl = $"{Globals.WEBPLAYER_BASE_URL}/track/{track.Id}",
                StoreUrl = track.Album.Url,
                ReleaseDate = track.Album.ReleaseDateOriginal.GetValueOrDefault().ToString("yyyy-MM-dd"),
                HiRes = track.Hires.GetValueOrDefault()
            });
        }

        private void FillResultsTablePanel<T>(IEnumerable<T> items, Func<T, SearchResultRow> createSearchResultRow)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                CreateResultRow(createSearchResultRow(item));
            }
        }

        public void CreateResultRow(SearchResultRow result)
        {
            resultsTableLayoutPanel.RowCount++;
            resultsTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            var currentRow = resultsTableLayoutPanel.RowCount - 1;

            var rowColor = GetResultRowColor(currentRow);

            var thumbnail = CreateThumbnail(result.ThumbnailUrl, rowColor);
            resultsTableLayoutPanel.Controls.Add(thumbnail, 0, currentRow);

            var secondColumnPanel = CreateReleaseInfoColumn(result, rowColor);
            resultsTableLayoutPanel.Controls.Add(secondColumnPanel, 1, currentRow);

            var thirdColumnPanel = CreateQualityColumn(result, rowColor);
            resultsTableLayoutPanel.Controls.Add(thirdColumnPanel, 2, currentRow);

            var selectButton = CreateDownloadButton(result.WebPlayerUrl);
            resultsTableLayoutPanel.Controls.Add(selectButton, 3, currentRow);
        }

        private Color GetResultRowColor(int currentRow)
        {
            return currentRow % 2 == 1 ? Color.FromArgb(33, 33, 33) : Color.FromArgb(45, 45, 45);
        }

        private PictureBox CreateThumbnail(string thumbnailUrl, Color rowColor)
        {
            return new PictureBox
            {
                ImageLocation = thumbnailUrl,
                SizeMode = PictureBoxSizeMode.CenterImage,
                Size = new Size(50, 50),
                Anchor = AnchorStyles.None,
                Dock = DockStyle.Fill,
                BackColor = rowColor
            };
        }

        private TableLayoutPanel CreateReleaseInfoColumn(SearchResultRow result, Color rowColor)
        {
            // Create fonts
            var detailsFont = FontManager.CreateFont("Hanken Grotesk Medium", 10);
            var titleFont = FontManager.CreateFont("Hanken Grotesk ExtraBold", 13, FontStyle.Bold);
            var artistFont = FontManager.CreateFont("Hanken Grotesk", 11, FontStyle.Bold | FontStyle.Italic);

            // Create TextBox controls
            var titleTextBox = CreateTextBox(result.Title, true, rowColor, Color.White, titleFont, BorderStyle.None);
            var durationTextBox = CreateTextBox(result.FormattedDuration, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);
            var artistTextBox = CreateTextBox(result.Artist, true, rowColor, Color.WhiteSmoke, artistFont, BorderStyle.None);
            var releaseDateTextBox = CreateTextBox(result.ReleaseDate, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);
            var tracks = CreateTracksTextBox(result.TrackCount, rowColor, detailsFont);

            // Resize TextBox controls fitting text in given font (estimation)
            ResizeControlForText(titleTextBox, 0, 500);
            ResizeControlForText(artistTextBox, 5);

            // Create titlePanel
            var titlePanel = CreateTitlePanel(titleTextBox, result.Explicit);

            // Create storeLink and webLink
            var storeLink = CreateLinkLabel("Preview in Store", result.StoreUrl, Color.FromArgb(0, 112, 239), Color.Blue, rowColor);
            var webLink = CreateLinkLabel("Preview in Web Player", result.WebPlayerUrl, Color.FromArgb(0, 112, 239), Color.Blue, rowColor);

            // Add storeLink and webLink functionality
            AddLinkLabelFunctionality(storeLink);
            AddLinkLabelFunctionality(webLink);

            // Create releaseInfoColumnPanel Control and add child Controls
            return CreateReleaseInfoPanel(rowColor, titlePanel, durationTextBox, artistTextBox, releaseDateTextBox, tracks, webLink, storeLink);
        }

        private TextBox CreateTracksTextBox(int trackCount, Color rowColor, Font detailsFont)
        {
            if (trackCount <= 0)
            {
                return null;
            }

            var trackText = trackCount != 1 ? $"{trackCount} Tracks" : $"{trackCount} Track";

            return CreateTextBox(trackText, true, rowColor, Color.WhiteSmoke, detailsFont, BorderStyle.None, HorizontalAlignment.Right);
        }

        private FlowLayoutPanel CreateTitlePanel(TextBox titleTextBox, bool isExplicit)
        {
            var titlePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Margin = new Padding(0),
                Controls = { titleTextBox },
                //BorderStyle = BorderStyle.FixedSingle
            };
            ControlTools.SetDoubleBuffered(titlePanel);

            if (!isExplicit)
            {
                return titlePanel;
            }

            var explicitLabel = CreateLabel("E", Color.FromArgb(75, 75, 75), Color.OrangeRed, FontManager.CreateFont("Hanken Grotesk ExtraBold", 8, FontStyle.Bold), BorderStyle.None, new Padding(5, 0, 0, 0), AnchorStyles.None);

            // Add tooltip for "Explicit"
            var toolTip = new ToolTip();
            toolTip.SetToolTip(explicitLabel, "Explicit");

            // Resize titleTextBox to make room for "Explicit"
            var finalExplicitLabelWidth = explicitLabel.GetPreferredSize(new Size(0, 0)).Width + explicitLabel.Margin.Left + explicitLabel.Margin.Right;
            titleTextBox.Width = 500 - finalExplicitLabelWidth;
            titlePanel.Controls.Add(explicitLabel);

            return titlePanel;
        }

        private FlowLayoutPanel CreateLinksPanel(Color rowColor, LinkLabel webLink, LinkLabel storeLink)
        {
            var linksPanel = new FlowLayoutPanel
            {
                BackColor = rowColor,
                AutoSize = true,
                Margin = new Padding(0),
            };
            ControlTools.SetDoubleBuffered(linksPanel);

            if (webLink != null)
            {
                linksPanel.Controls.Add(webLink);
            }

            if (storeLink != null)
            {
                linksPanel.Controls.Add(storeLink);
            }

            return linksPanel;
        }

        private TableLayoutPanel CreateReleaseInfoPanel(Color rowColor, FlowLayoutPanel titlePanel, TextBox durationTextBox, TextBox artistTextBox, TextBox releaseDateTextBox, TextBox tracks, LinkLabel webLink, LinkLabel storeLink)
        {
            var releaseInfoColumnPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = rowColor,
                ColumnStyles = { new ColumnStyle(SizeType.Absolute, 500), new ColumnStyle(SizeType.Absolute, 80) }
            };
            ControlTools.SetDoubleBuffered(releaseInfoColumnPanel);

            // Add controls to releaseInfoColumnPanel
            releaseInfoColumnPanel.Controls.Add(titlePanel, 0, 0);
            releaseInfoColumnPanel.Controls.Add(durationTextBox, 1, 0);
            releaseInfoColumnPanel.Controls.Add(artistTextBox, 0, 1);
            releaseInfoColumnPanel.Controls.Add(releaseDateTextBox, 1, 1);

            // Add tracks TextBox if it exists
            if (tracks != null)
            {
                releaseInfoColumnPanel.Controls.Add(tracks, 1, 2);
            }

            if (webLink == null && storeLink == null)
            {
                return releaseInfoColumnPanel;
            }

            // Create linksPanel when at least 1 link exists
            var linksPanel = CreateLinksPanel(rowColor, webLink, storeLink);
            releaseInfoColumnPanel.Controls.Add(linksPanel, 0, 2);

            return releaseInfoColumnPanel;
        }

        private TableLayoutPanel CreateQualityColumn(SearchResultRow result, Color rowColor)
        {
            var quality = CreateTextBox(result.FormattedQuality, true, rowColor, Color.White, FontManager.CreateFont("Hanken Grotesk Medium", 10), BorderStyle.None);
            PictureBox hiResIcon = null;

            if (result.HiRes)
            {
                hiResIcon = new PictureBox
                {
                    Image = Properties.Resources.smr_audio,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(30, 30),
                    Margin = new Padding(0, 0, 0, 0),
                    Anchor = AnchorStyles.Top
                };
            }

            var qualityColumnPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = hiResIcon == null ? 1 : 2,
                BackColor = rowColor,
                ColumnStyles = { new ColumnStyle(SizeType.Percent, 100) },
                RowStyles = { new RowStyle(SizeType.AutoSize), new RowStyle(SizeType.AutoSize) }
            };
            ControlTools.SetDoubleBuffered(qualityColumnPanel);

            qualityColumnPanel.Controls.Add(quality, 0, 0);
            if (hiResIcon != null)
            {
                qualityColumnPanel.Controls.Add(hiResIcon, 0, 1);
            }

            return qualityColumnPanel;
        }

        private Button CreateDownloadButton(string webPlayerUrl)
        {
            var downloadButton = new Button
            {
                Text = "Download",
                BackColor = Color.FromArgb(0, 112, 239),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Height = 23,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };
            downloadButton.FlatAppearance.BorderSize = 0;
            downloadButton.Click += (sender, e) =>
            {
                HandleDownload(webPlayerUrl);
                downloadButton.BackColor = Color.ForestGreen;
            };

            return downloadButton;
        }

        private void HandleDownload(string webPlayerUrl)
        {
            try
            {
                // Start the download, or queue it if it's busy.
                if (Globals.QbdlxForm.DownloadManager.IsBusy)
                {
                    if (!Globals.QbdlxForm.DownloadQueue.Contains(webPlayerUrl))
                    {
                        Globals.QbdlxForm.DownloadQueue.Enqueue(webPlayerUrl);
                        Globals.QbdlxForm.UpdateQueueLabel();
                    }
                }
                else
                {
                    // Copy selected download link to the main form link field
                    Globals.QbdlxForm.downloadUrl.Invoke(new Action(() => Globals.QbdlxForm.downloadUrl.Text = webPlayerUrl));

                    // Start download from the main form
                    Task.Run(() => Globals.QbdlxForm.StartLinkItemDownloadAsync(webPlayerUrl)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, show an error message, etc.)
                var errorLines = new List<string>
                {
                    $"Error starting download for {webPlayerUrl}",
                    ex.ToString(),
                    ""
                };

                // Write detailed info to log
                File.AppendAllLines(errorLog, errorLines);

                // Show an error message to the user
                MessageBox.Show($"Error starting download for {webPlayerUrl}\nlog saved to {errorLog}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TextBox CreateTextBox(string text, bool readOnly, Color backColor, Color foreColor, Font font, BorderStyle borderStyle, HorizontalAlignment textAlign = HorizontalAlignment.Left)
        {
            return new TextBox
            {
                Text = text,
                ReadOnly = readOnly,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = font,
                BorderStyle = borderStyle,
                Margin = new Padding(0),
                AutoSize = false,
                TextAlign = textAlign
            };
        }

        private System.Windows.Forms.Label CreateLabel(string text, Color backColor, Color foreColor, Font font, BorderStyle borderStyle, Padding margin = default, AnchorStyles anchor = AnchorStyles.None, ContentAlignment textAlign = ContentAlignment.TopLeft)
        {
            return new System.Windows.Forms.Label
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = font,
                BorderStyle = borderStyle,
                Margin = margin,
                Anchor = anchor,
                TextAlign = textAlign,
                AutoSize = true
            };
        }

        private LinkLabel CreateLinkLabel(string text, string url, Color linkColor, Color activeLinkColor, Color backColor)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return new LinkLabel
            {
                Text = text,
                Tag = url,
                Font = FontManager.CreateFont("Hanken Grotesk Medium", 9),
                AutoSize = true,
                LinkBehavior = LinkBehavior.HoverUnderline,
                LinkColor = linkColor,
                ActiveLinkColor = activeLinkColor,
                BackColor = backColor
            };
        }

        private void AddLinkLabelFunctionality(LinkLabel linkLabel)
        {
            if (linkLabel?.Tag == null)
            {
                return;
            }

            // Get URL from Tag attribute
            var url = linkLabel.Tag.ToString();

            // Open the URL in the default browser when clicked
            linkLabel.LinkClicked += (sender, e) => Process.Start(url);

            // Add tooltip with the URL
            var toolTip = new ToolTip();
            toolTip.SetToolTip(linkLabel, url);

            // Add context menu with "Copy URL" option
            var contextMenu = new ContextMenu();
            var copyUrlItem = new MenuItem("Copy URL");
            copyUrlItem.Click += (sender, e) => CopyToClipboard(url);
            contextMenu.MenuItems.Add(copyUrlItem);
            linkLabel.ContextMenu = contextMenu;
        }

        private void ResizeControlForText(Control control, int extraMargin = 0, int fixedWidth = 0)
        {
            var preferredSize = TextRenderer.MeasureText(control.Text, control.Font);
            var newWidth = (fixedWidth == 0) ? preferredSize.Width + extraMargin : fixedWidth;
            control.Size = new Size(newWidth, preferredSize.Height);
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            searchButton.Enabled = false;
            containerScrollPanel.Hide();

            // In case of an exception, a newline is added to the searchQuery string for some reason.
            // So we clone the object to prevent newline in TextBox on error
            var searchQuery = searchInput.Text.Clone().ToString();

            // Clear previous results before fetching new results
            ResetResultsTableLayoutPanel();

            if (string.IsNullOrEmpty(searchQuery))
            {
                containerScrollPanel.Show();
                searchButton.Enabled = true;

                return;
            }

            try
            {
                switch (searchTypeSelect.Text)
                {
                    case "Album":
                        {
                            var albumsResult = QobuzApiServiceManager.GetApiService().SearchAlbums(searchQuery, 100, 0, true);
                            Fill_AlbumResultsTablePanel(albumsResult);

                            break;
                        }

                    case "Track":
                        {
                            var tracksResult = QobuzApiServiceManager.GetApiService().SearchTracks(searchQuery, 100, 0, true);
                            Fill_TrackResultsTablePanel(tracksResult);

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                ShowAndLogSearchResultError(ex);
            }

            containerScrollPanel.Show();
            searchButton.Enabled = true;
        }

        private void SearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Nothing yet
        }

        private void SearchForm_Load(object sender, EventArgs e)
        {
            // Nothing yet
        }

        // Enable moving Form with mouse in absence of titlebar
        private void SearchForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void SearchInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchButton_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // Alternating row colors for search results
        private void ResultsTableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(GetResultRowColor(e.Row)), e.CellBounds);
        }

        private void CopyToClipboard(string text)
        {
            Task.Run(() =>
            {
                var thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            });
        }
    }
}