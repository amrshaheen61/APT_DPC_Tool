using APT_DPC_Tool.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
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

namespace APT_DPC_Tool
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            Log.progress = new Progress<object>(text => textBox1.AppendText(text.ToString()));
            comboBox1.DataSource= Enum.GetValues(typeof(Game));
            
            //textBox1.Location = new Point(textBox1.Location.X, textBox1.Location.Y - panel2.Height);
            //textBox1.Height += panel2.Height;
        }

        void DisableControls()
        {
            textBox1.Text = "";
            button1.Enabled = false;
            button2.Enabled = false;
            comboBox1.Enabled = false;
            button3.Enabled = false;
            COMMON_DPC.Enabled = false;
        }
        void EnableControls()
        {
            button1.Enabled = true;
            button2.Enabled = true;
            comboBox1.Enabled = true;
            button3.Enabled = true;
            COMMON_DPC.Enabled = true;
        }


        private async void button1_Click(object sender, EventArgs e)
        {
    
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select DPC File:";
            fileDialog.Filter = "DPC file (*.DPC)|*.DPC";


            CommonOpenFileDialog FolderDialog = new CommonOpenFileDialog();
            FolderDialog.IsFolderPicker = true;

            if (fileDialog.ShowDialog() == DialogResult.OK && FolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DisableControls();
                string FilePath = fileDialog.FileName;
                string FolderPath = FolderDialog.FileName;
                FolderPath = FolderPath + "\\" + Path.GetFileNameWithoutExtension(FilePath) + "\\";

                string CommonPath = @"E:\Games\A Plague Tale Requiem\DATAS\COMMON.DPC";

               Dpc dpc = new Dpc(FilePath,(Game)comboBox1.SelectedItem,COMMON_DPC.Text);
                try
                {
                 await Task.Run( () => dpc.Unpack(FolderPath));
                }catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,"Error",MessageBoxButtons.OK,MessageBoxIcon.Stop);
                    EnableControls();
                    return;
                }
                MessageBox.Show("Done!");
                EnableControls();
            }
     
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "Select DPC File:";
            fileDialog.Filter = "DPC file (*.DPC)|*.DPC";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            { 
                COMMON_DPC.Text=fileDialog.FileName;
            }
         }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
     
            if ((Game)comboBox1.SelectedItem == Game.Requiem)
            {
                panel2.Visible = true;
              //  textBox1.Location = new Point(point.X, point.Y + panel2.Height);
                // textBox1.Height -= panel2.Height;
             
            }
            else
            {
                panel2.Visible = false;
              //  textBox1.Location = new Point(point.X, point.Y - panel2.Height);
            }
         
        }

        private async void button2_Click(object sender, EventArgs e)
        {


            OpenFileDialog XmlFile = new OpenFileDialog();
            XmlFile.Title = "Select \"FilesMap.Xml\" File:";
            XmlFile.Filter = "DPC file (FilesMap.Xml)|FilesMap.Xml";
            string XmlPath;
            if (XmlFile.ShowDialog() == DialogResult.OK) 
            {
                 XmlPath = XmlFile.FileName;
            }
            else
            {
                return;
            }


            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Title = "Save DPC File:";
            fileDialog.Filter = "DPC file (*.DPC)|*.DPC";
            
            fileDialog.FileName = Path.GetFileName(Path.GetDirectoryName(XmlPath)+ "_NEW.DPC");
            fileDialog.InitialDirectory = Path.GetFullPath( Path.Combine( Path.GetDirectoryName(XmlPath), @"..\"));

            if (fileDialog.ShowDialog() == DialogResult.OK )
            {
                DisableControls();
                string FilePath = fileDialog.FileName;

                Dpc dpc = new Dpc(FilePath, (Game)comboBox1.SelectedItem, COMMON_DPC.Text);
                try
                {
                    await Task.Run(() => dpc.Pack(XmlPath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    EnableControls();
                    return;
                }
                MessageBox.Show("Done!");
                EnableControls();
            }

        }
    }
}
