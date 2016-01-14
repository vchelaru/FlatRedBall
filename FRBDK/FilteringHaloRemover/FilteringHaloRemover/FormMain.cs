using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace FilteringHaloRemover
{
    public partial class FormMain : Form
    {
        private string fullPath = "";
        private string fileName = "";
        private string fileDirectory = "";

        //private string batchFolder = "";

        private string[] mFilesToWork;

        Color[,] colorArray;

        public delegate void VoidDelegate();

        public FormMain()
        {
            InitializeComponent();
        }

       

        private void CenterForm(Form formToCenter)
        {
            Point centerLocation = new Point();

            centerLocation.X = this.Location.X + ((this.Width - formToCenter.Width) / 2);
            centerLocation.Y = this.Location.Y + ((this.Height - formToCenter.Height) / 2);
            formToCenter.Location = centerLocation;
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.AddExtension = true;
            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            openFileDialog1.Filter = "PNG images (*.png)|*.png|All supported images|*.png,*.jpg";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                mFilesToWork = null;

                LoadFile(openFileDialog1.FileName);
            }
        }

        /// <summary>
        /// Loads the file into the preview window and sets the local variables
        /// </summary>
        /// <param name="fullPath"></param>
        private void LoadFile(string fullPath)
        {
            this.fullPath = fullPath;     

            this.fileName = Path.GetFileName(this.fullPath);
            this.fileDirectory = Path.GetDirectoryName(this.fullPath);

            pictureBoxPreview.Load(this.fullPath);
        }

       


        private void button1_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(fullPath) && mFilesToWork == null)
            {
                MessageBox.Show("You must select an image or batch folder to process.");
                return;
            }

           
            DateTime startTime = DateTime.Now;

            FormProgress formProgress = new FormProgress();
            formProgress.Show();
            CenterForm(formProgress);

            Application.DoEvents();

            if (fullPath.Length > 0)
            {
                formProgress.ProgressBarTotal.Maximum = 1;

                Stream fs = new StreamReader(fullPath).BaseStream;
                Bitmap bitmapOriginal = new Bitmap(Bitmap.FromStream(fs));
                fs.Close();
                fs.Dispose();
                //set the process bar                
                formProgress.ProgressBar.Maximum = bitmapOriginal.Width * 2;
                ProcessImage(bitmapOriginal, this.fullPath, formProgress.DoProgress, formProgress.DoTotalProgress);

            }
            else if (mFilesToWork != null)
            {
                if (mFilesToWork.Length > 0)
                {
                    formProgress.ProgressBarTotal.Maximum = mFilesToWork.Length;

                    foreach (string pathToFile in mFilesToWork)
                    {
                        if (File.Exists(pathToFile))
                        {
                            Stream fs = new StreamReader(pathToFile).BaseStream;
                            Bitmap bitmapOriginal = new Bitmap(Bitmap.FromStream(fs));
                            fs.Close();
                            fs.Dispose();
                            //set the process bar                
                            formProgress.ProgressBar.Value = 0;
                            formProgress.ProgressBar.Maximum = bitmapOriginal.Width * 2;
                            ProcessImage(bitmapOriginal, pathToFile, formProgress.DoProgress, formProgress.DoTotalProgress);
                        }
                        else
                        {
                            //file must have moved, decrement the total max.
                            formProgress.ProgressBarTotal.Maximum--;
                        }
                        
                    }
                }
                else
                {
                    formProgress.ProgressBar.Maximum = 1;
                    formProgress.ProgressBar.Value = 1;
                    formProgress.ProgressBarTotal.Maximum = 1;
                    formProgress.ProgressBarTotal.Value = 1;
                    MessageBox.Show("No PNG files found in the specified folder");
                }                
            }
            else
            {
                MessageBox.Show("Error: no image or folder to process");
            }

            formProgress.EnableOK = true;
            //MessageBox.Show(DateTime.Now.Subtract(startTime).TotalMilliseconds.ToString());
        }

        private void ProcessImage(Bitmap imageToProcess, string fullPath, VoidDelegate progressMethod, VoidDelegate totalProgressMethod)
        {
            //load the color array
            LoadColorArray(imageToProcess, progressMethod);

            Bitmap bitmapProcessed = RemoveFilteringHalo2(imageToProcess, progressMethod);
            
            string savePath = checkBoxReplace.Checked? fullPath : Path.Combine(Path.GetDirectoryName(fullPath), String.Format("FHR_{0}", Path.GetFileName(fullPath)));

            try
            {
                bitmapProcessed.Save(savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Could not save image '{0}'.  Make sure it is not being used by another process (e.g. Photoshop, GIMP.)  Error Message: {1}", savePath,ex.Message));
            }

            //increment the total
            totalProgressMethod();
        }

        private void LoadColorArray(Bitmap bitmap, VoidDelegate progressMethod)
        {
            colorArray = new Color[bitmap.Width,bitmap.Height];

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    colorArray[x,y] = bitmap.GetPixel(x, y);
                    
                }

                progressMethod();
            }

        }

        private Bitmap RemoveFilteringHalo2(Bitmap bitmap, VoidDelegate progressMethod)
        {
            Color tmpColor;
            Bitmap bitmapProcessed = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    tmpColor = colorArray[x,y];
                    //if the pixel is transparent, then process it.
                    if (tmpColor.A == 0)
                    {
                        bitmapProcessed.SetPixel(x, y, DeterminePixelColorAverage(x, y, tmpColor));
                    }
                }
                //progress on column change
                progressMethod();
                Application.DoEvents();
            }

            return bitmapProcessed;
        }


        /// <summary>
        /// Returns the color the supplied pixel should be by investigating surrounding pixels.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Color DeterminePixelColor2(int x, int y, Color originalColor)
        {
            //@kao TODO
            Color tmpColor;


            //set up the processing flags
            bool canProcessRight = (x < (colorArray.GetLength(0) - 1));
            bool canProcessLeft = (x > 0);
            bool canProcessBelow = (y < (colorArray.GetLength(1) - 1));
            bool canProcessAbove = (y > 0);

            //get the adjacent pixels in priority order and set the color accordingly and return
            //right first
            if (canProcessRight)
            {
                tmpColor = colorArray[(x + 1), y];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //left
            if (canProcessLeft)
            {
                tmpColor = colorArray[(x - 1), y];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //bottom
            if (canProcessBelow)
            {
                tmpColor = colorArray[x, (y + 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //top
            if (canProcessAbove)
            {
                tmpColor = colorArray[x, (y - 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //corners
            //upper-right
            if (canProcessRight && canProcessAbove)
            {
                tmpColor = colorArray[(x + 1), (y - 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //lower-right
            if (canProcessRight && canProcessBelow)
            {
                tmpColor = colorArray[(x + 1), (y + 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //upper-left
            if (canProcessLeft && canProcessAbove)
            {
                tmpColor = colorArray[(x - 1),(y - 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //lower-left
            if (canProcessLeft && canProcessBelow)
            {
                tmpColor = colorArray[(x - 1), (y + 1)];
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //if nothign found, return the original color
            return originalColor;
            //return Color.Black;

        }

        /// <summary>
        /// Returns the color the supplied pixel should be by averaging the surrounding pixel color values of non transparent pixels.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Color DeterminePixelColorAverage(int x, int y, Color originalColor)
        {
            //@kao TODO
            int R = 0;
            int G = 0;
            int B = 0;
            int count = 0;

            List<Color> colorList = new List<Color>();

            bool canProcessRight = (x < (colorArray.GetLength(0) - 1));
            bool canProcessLeft = (x > 0);

            if (canProcessLeft)
            {
                colorList.Add(colorArray[(x - 1), y]);

                try { colorList.Add(colorArray[(x - 1), (y - 1)]); }
                catch { }
                try { colorList.Add(colorArray[(x - 1), (y + 1)]); }
                catch { }
            }
            if (canProcessRight)
            {
                colorList.Add(colorArray[(x + 1), y]);
                try { colorList.Add(colorArray[(x + 1), (y - 1)]); }
                catch { }
                try { colorList.Add(colorArray[(x + 1), (y + 1)]); }
                catch { }
            }

            try { colorList.Add(colorArray[x, (y + 1)]); }
            catch { }
            try { colorList.Add(colorArray[x, (y - 1)]); }
            catch { }
            
            

            
            foreach (Color tmpC in colorList)
            {
                if (tmpC.A > 0)
                {
                    count++;
                    R += tmpC.R;
                    G += tmpC.G;
                    B += tmpC.B;
                }
            }

            //only return if one or more of the surrounding pixels were non-transparent.
            if (count > 0)
            {
                return Color.FromArgb(0, R / count, G / count, B / count);
                //return Color.FromArgb(255, R / count, G / count, B / count);
            }
            else
            {
                return originalColor;
                //return Color.Black;
            }
            
        }

        #region OLD METHODS

        private void buttonExtract_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(fullPath))
            {
                MessageBox.Show("You must select an image to process.");
                return;
            }


            string outputDirectory = this.fileDirectory;

            DateTime startTime;
            TimeSpan timeToProcess;

            startTime = DateTime.Now;

            Bitmap bitmapOriginal = new Bitmap(Bitmap.FromFile(fullPath));

            FormProgress formProgress = new FormProgress();
            formProgress.Show();
            CenterForm(formProgress);

            Application.DoEvents();

            //set the process bar
            formProgress.ProgressBar.Maximum = bitmapOriginal.Width * bitmapOriginal.Height;

            Bitmap bitmapProcessed = RemoveFilteringHalo(bitmapOriginal, formProgress.DoProgress);

            bitmapProcessed.Save(Path.Combine(outputDirectory, String.Format("FHR_{0}", fileName)));

            formProgress.EnableOK = true;

            timeToProcess = DateTime.Now.Subtract(startTime);
            MessageBox.Show(timeToProcess.TotalMilliseconds.ToString());
        }

        private Bitmap RemoveFilteringHalo(Bitmap bitmap, VoidDelegate progressMethod)
        {
            Color tmpColor;
            Bitmap bitmapProcessed = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat);

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    tmpColor = bitmap.GetPixel(x, y);
                    //if the pixel is transparent, then process it.
                    if (tmpColor.A == 0)
                    {
                        bitmapProcessed.SetPixel(x, y, DeterminePixelColor(bitmap, x, y, tmpColor));
                    }

                    //progress.
                    progressMethod();
                }
            }

            return bitmapProcessed;
        }

        /// <summary>
        /// Returns the color the supplied pixel should be by investigating surrounding pixels.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Color DeterminePixelColor(Bitmap bitmap, int x, int y, Color originalColor)
        {
            //@kao TODO
            Color tmpColor;


            //set up the processing flags
            bool canProcessRight = (x < (bitmap.Width - 1));
            bool canProcessLeft = (x > 0);
            bool canProcessBelow = (y < (bitmap.Height - 1));
            bool canProcessAbove = (y > 0);

            //get the adjacent pixels in priority order and set the color accordingly and return
            //right first
            if (canProcessRight)
            {
                tmpColor = bitmap.GetPixel(x + 1, y);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //left
            if (canProcessLeft)
            {
                tmpColor = bitmap.GetPixel(x - 1, y);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //bottom
            if (canProcessBelow)
            {
                tmpColor = bitmap.GetPixel(x, y + 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //top
            if (canProcessAbove)
            {
                tmpColor = bitmap.GetPixel(x, y - 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //corners
            //upper-right
            if (canProcessRight && canProcessAbove)
            {
                tmpColor = bitmap.GetPixel(x + 1, y - 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //lower-right
            if (canProcessRight && canProcessBelow)
            {
                tmpColor = bitmap.GetPixel(x + 1, y + 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //upper-left
            if (canProcessLeft && canProcessAbove)
            {
                tmpColor = bitmap.GetPixel(x - 1, y - 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //lower-left
            if (canProcessLeft && canProcessBelow)
            {
                tmpColor = bitmap.GetPixel(x - 1, y + 1);
                if (tmpColor.A > 0)
                {
                    return Color.FromArgb(0, tmpColor.R, tmpColor.G, tmpColor.B);
                }
            }
            //if nothign found, return the original color
            return originalColor;
            //return Color.Black;

        }
        #endregion

        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.SelectedPath == "")
            {
                folderBrowserDialog1.SelectedPath = Path.GetDirectoryName(Application.ExecutablePath);
            }

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.fullPath = "";
                this.fileDirectory = "";
                this.fileName = "";
                this.pictureBoxPreview.Image = null;

                string batchFolder = folderBrowserDialog1.SelectedPath;

                mFilesToWork = Directory.GetFiles(batchFolder, "*.png", SearchOption.AllDirectories);

                FileListBox.Items.Clear();

                FileListBox.Items.AddRange(mFilesToWork);
            }
        }

       
    }
}