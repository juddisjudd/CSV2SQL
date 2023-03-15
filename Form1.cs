using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CSV2SQL
{
    public partial class Form1 : Form
    {
        private string _selectedCsvFilePath;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";
            openFileDialog.FileName = "";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedCsvFilePath = openFileDialog.FileName;
                lblStatus.Text = $"Selected file: {Path.GetFileName(_selectedCsvFilePath)}";
            }
        }

        private async void btnConvert_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_selectedCsvFilePath))
            {
                saveFileDialog.Filter = "ZIP files (*.zip)|*.zip";
                saveFileDialog.FileName = "output.zip";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    btnConvert.Enabled = false;
                    progressBar.Style = ProgressBarStyle.Marquee;
                    lblStatus.Text = "Converting...";

                    try
                    {
                        await ConvertCsvToMariaDBAsync(_selectedCsvFilePath, saveFileDialog.FileName);

                        lblStatus.Text = $"Conversion completed. Output saved as '{saveFileDialog.FileName}'.";
                    }
                    catch (Exception ex)
                    {
                        lblStatus.Text = $"Conversion failed: {ex.Message}";
                    }
                    finally
                    {
                        progressBar.Style = ProgressBarStyle.Continuous;
                        btnConvert.Enabled = true;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a .csv file to convert.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task ConvertCsvToMariaDBAsync(string csvFilePath, string outputFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    using (var fileStream = File.OpenRead(csvFilePath))
                    {
                        using (var fileContent = new StreamContent(fileStream))
                        {
                            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                            {
                                Name = "files[]",
                                FileName = Path.GetFileName(csvFilePath)
                            };

                            form.Add(fileContent);
                            HttpResponseMessage response = await httpClient.PostAsync("https://www.rebasedata.com/api/v1/convert?outputFormat=mariadb&errorResponse=zip", form);

                            response.EnsureSuccessStatusCode();

                            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                            File.WriteAllBytes(outputFilePath, fileBytes);
                        }
                    }
                }
            }
        }
    }
}
