using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace XRCultureRegisterViewerTool
{
    public partial class RegisterViewerForm : Form
    {
        public RegisterViewerForm()
        {
            InitializeComponent();
        }

        private void RegisterViewerForm_Load(object sender, EventArgs e)
        {
            _textBoxMiddleware.Text = "http://localhost:5253/";
        }

        async private void _buttonRegister_Click(object sender, EventArgs e)
        {
            // Reset the session token and button state
            SessionToken = null;
            _buttonAuthorize.Enabled = false;
            _textBoxLog.Text = string.Empty;

            // Validate the middleware URL input
            if (string.IsNullOrWhiteSpace(_textBoxMiddleware.Text))
            {
                MessageBox.Show("Please enter the middleware URL.");
                return;
            }

            // Validate the middleware URL
            if (!Uri.TryCreate(_textBoxMiddleware.Text, UriKind.Absolute, out Uri? middlewareUri) || !middlewareUri.IsWellFormedOriginalString())
            {
                MessageBox.Show("Please enter a valid middleware URL.");
                return;
            }
            // Check if the scheme is either http or https
            if (!middlewareUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !middlewareUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please enter a valid middleware URL with http or https scheme.");
                return;
            }

            var openFileDialog = new OpenFileDialog()
            {
                FileName = "XML file",
                Filter = "XML files (*.xml)|*.xml",
                Title = "Open XML file"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var streamReader = new StreamReader(openFileDialog.FileName);
                    var registerRequest = streamReader.ReadToEnd();

                    using (HttpClient client = new HttpClient())
                    {
                        var url = _textBoxMiddleware.Text + "Registry?handler=Register";
                        var content = new StringContent(JsonConvert.SerializeObject($"{registerRequest}"), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(url, content);

                        string responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseString);

                        _textBoxLog.Text = responseString;

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseString);

                        var status = xmlDoc.SelectSingleNode("//Status")?.InnerText;
                        if (status?.Trim() == "202")
                        {
                            SessionToken = xmlDoc.SelectSingleNode("//ServiceToken")?.InnerText;
                            _buttonAuthorize.Enabled = !string.IsNullOrEmpty(SessionToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error:\n\n{ex.Message}\n\n" +
                        $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        async private void _buttonAuthorize_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                FileName = "XML file",
                Filter = "XML files (*.xml)|*.xml",
                Title = "Open XML file"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var streamReader = new StreamReader(openFileDialog.FileName);
                    var authorizeRequest = streamReader.ReadToEnd().Replace("%SESSION_TOKEN%", SessionToken);

                    using (HttpClient client = new HttpClient())
                    {
                        var url = _textBoxMiddleware.Text + "Registry?handler=Authorize";
                        var content = new StringContent(JsonConvert.SerializeObject($"{authorizeRequest}"), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(url, content);

                        string responseString = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(responseString);

                        _textBoxLog.Text = responseString;

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(responseString);

                        var status = xmlDoc.SelectSingleNode("//Status")?.InnerText;
                        if (status?.Trim() == "200")
                        {
                            MessageBox.Show($"Status:\n\n{status}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error:\n\n{ex.Message}\n\n" +
                        $"Details:\n\n{ex.StackTrace}");
                }
                finally
                {
                    SessionToken = null;
                    _buttonAuthorize.Enabled = false;
                }
            }
        }

        private void _buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void _buttonViewModel_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                FileName = "BINZ file",
                Filter = "BINZ files (*.binz)|*.binz",
                Title = "Open BINZ file"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    //#todo Midlleware - get viewer endpoint

                    //var streamReader = new StreamReader(openFileDialog.FileName);
                    //var authorizeRequest = streamReader.ReadToEnd().Replace("%SESSION_TOKEN%", SessionToken);

                    //using (HttpClient client = new HttpClient())
                    //{
                    //    var url = _textBoxMiddleware.Text + "Registry?handler=Authorize";
                    //    var content = new StringContent(JsonConvert.SerializeObject($"{authorizeRequest}"), Encoding.UTF8, "application/json");

                    //    HttpResponseMessage response = await client.PostAsync(url, content);

                    //    string responseString = await response.Content.ReadAsStringAsync();
                    //    Console.WriteLine(responseString);

                    //    _textBoxLog.Text = responseString;

                    //    XmlDocument xmlDoc = new XmlDocument();
                    //    xmlDoc.LoadXml(responseString);

                    //    var status = xmlDoc.SelectSingleNode("//Status")?.InnerText;
                    //    if (status?.Trim() == "200")
                    //    {
                    //        MessageBox.Show($"Status:\n\n{status}");
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error:\n\n{ex.Message}\n\n" +
                        $"Details:\n\n{ex.StackTrace}");
                }
                finally
                {
                    SessionToken = null;
                    _buttonAuthorize.Enabled = false;
                }
            }

        // xml request for Viewer REST Service
        string viewModelRequest =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ViewModelRequest>
    <Name>%NAME%</Name>
    <Parameters></Parameters>
</ViewModelRequest>";
            viewModelRequest = viewModelRequest.Replace("%NAME%", Path.GetFileName(openFileDialog.FileName));//#todo

            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(10);

                    var viewerUrl = "http://localhost:5178/" + "Viewer?handler=ViewModel"; // #todo get it from middleware
                    using (var form = new MultipartFormDataContent())
                    {
                        // Add XML request as a form part
                        form.Add(new StringContent(viewModelRequest, Encoding.UTF8, "application/xml"), "request", "request.xml");

                        // Add the zip file as a form part
                        using (var fileStream = File.OpenRead(openFileDialog.FileName))
                        {
                            if (fileStream == null || fileStream.Length == 0)
                                throw new Exception("File stream is null or empty. Please check the file path.");

                            var fileContent = new StreamContent(fileStream);
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
                            form.Add(fileContent, "file", Path.GetExtension(openFileDialog.FileName));

                            var response = await RetryPostAsync(client, viewerUrl, form);
                            string responseString = await response.Content.ReadAsStringAsync();

                            var xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(responseString);

                            var status = xmlDoc.SelectSingleNode("//Status")?.InnerText;
                            if (status?.Trim() != "200")
                            {
                                throw new Exception($"Viewer REST Service returned error: {status}");
                            }

                            var modelUrl = xmlDoc.SelectSingleNode("//Parameters/URL")?.InnerText;
                            if (string.IsNullOrEmpty(modelUrl))
                            {
                                throw new Exception("Viewer REST Service did not return a 'URL'.");
                            }

                            // Open the URL in the default browser
                            try
                            {
                                // Handle different OS platforms correctly
                                if (OperatingSystem.IsWindows())
                                {
                                    Process.Start(new ProcessStartInfo(modelUrl) { UseShellExecute = true });
                                }
                                else if (OperatingSystem.IsLinux())
                                {
                                    Process.Start("xdg-open", modelUrl);
                                }
                                else if (OperatingSystem.IsMacOS())
                                {
                                    Process.Start("open", modelUrl);
                                }
                                else
                                {
                                    MessageBox.Show($"URL is available but cannot be opened automatically on this platform: {modelUrl}");
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error opening browser: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

        // Retry method for the HTTP POST
        private async Task<HttpResponseMessage?> RetryPostAsync(HttpClient client, string url, HttpContent content, int maxRetries = 3)
        {
            int retryCount = 0;
            HttpResponseMessage? response = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    response = await client.PostAsync(url, content);
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw;

                    int delayMs = (int)Math.Pow(2, retryCount) * 1000; // Exponential backoff
                    await Task.Delay(delayMs);

                    // Log retry attempt
                    Debug.WriteLine("Retry {Count}/{Max} for {Url} after error: {Error}",
                        retryCount, maxRetries, url, ex.Message);
                }
            }

            return response;
        }

        private string? SessionToken { get; set; }
    }
}
