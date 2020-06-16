using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MetroFramework.Forms;


namespace FaceID
{
    public partial class Report : MetroForm
    {
        public Report()
        {
            InitializeComponent();
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            Data.IsClickToReport = false;
            Close();
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            DataGridView report_gridview = new DataGridView();
            DataTable report_table = new DataTable();
            report_gridview.Size = new Size(290, 352);
            report_gridview.Location = new Point(23, 75);
            if (Data.IsClickToReport == true)
            {
                if (File.Exists(Application.StartupPath + "/report.csv"))
                {
                    string[] raw_text = File.ReadAllLines(Application.StartupPath + "/report.csv");
                    string[] data_col = null;
                    int x = 0;
                    foreach (string text_line in raw_text)
                    {
                        data_col = text_line.Split(',');
                        if (x == 0)
                        {
                            for (int i = 0; i <= data_col.Count() - 1; i++)
                            {
                                report_table.Columns.Add(data_col[i]);
                            }
                            x++;
                        }
                        else
                        {
                            report_table.Rows.Add(data_col);
                        }
                    }
                    report_gridview.DataSource = report_table;
                    Controls.Add(report_gridview);
                    report_gridview.AutoResizeColumns();
                }
                else
                {
                    MessageBox.Show("Похоже, что отчёт был удален или не был создан. Повторное создание...", "Невозможно отобразить отчёт", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    File.Delete(Application.StartupPath + "/report.csv");
                    File.WriteAllText(Application.StartupPath + "/report.csv", "№,Name,Date and Time \n");
                }
            }
        }
    }
}
