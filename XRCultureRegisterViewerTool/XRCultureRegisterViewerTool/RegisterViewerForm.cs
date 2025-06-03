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
                    var filePath = openFileDialog.FileName;
                    var streamReader = new StreamReader(openFileDialog.FileName);
                    var registerRequest = streamReader.ReadToEnd();

                    //var jsonObject = new { body = registerRequest };
                    //var content = new StringContent(JsonConvert.SerializeObject(jsonObject), Encoding.UTF8, "application/json");


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
                            ServiceToken = xmlDoc.SelectSingleNode("//ServiceToken")?.InnerText;
                        }
                    }
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void _buttonAuthorize_Click(object sender, EventArgs e)
        {
            _textBoxMiddleware.Text = "http://localhost:5253/";
        }

        private void _buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void sendAuthorizationRequest(string serviceToken)
        {
            using (HttpClient client = new HttpClient())
            {
                var url = _textBoxMiddleware.Text + "Registry?handler=Authorize";
                var content = new StringContent(JsonConvert.SerializeObject(new { ServiceToken = serviceToken }), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync(url, content).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(responseString);
                _textBoxLog.Text = responseString;
            }
        }

        private string? ServiceToken { get; set; }
    }
}
