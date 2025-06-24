using Newtonsoft.Json;
using System.Diagnostics;
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

        private string? SessionToken { get; set; }
    }
}
