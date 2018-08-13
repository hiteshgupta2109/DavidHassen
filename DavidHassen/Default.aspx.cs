using DavidHassen.Shared;
using DavidHassen.BLL;
using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web.Hosting;
using System.Web.UI;
using SD = System.Drawing;


namespace DavidHassen
{
    public partial class _Default : Page
    {
        #region Dependency Injection

        private IImageService imageService;
        /// <summary>
        /// Default constructor for resolve service dependency
        /// </summary>
        public _Default()
        {
            imageService = new ImageService(); ;
        }
        String path = System.Web.HttpContext.Current.Request.PhysicalApplicationPath + "Images\\";

        #endregion

        /// <summary>
        /// Page Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Button click event for Upload image to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            var FileSaved = false;
            // Check if uploader has a file.
            if (ImageUploader.HasFile)
            {
                //Session["UploadImage"] = ImageUploader.FileName;
                String fileExtension = Path.GetExtension(ImageUploader.FileName).ToLower();
                Session["UploadImage"] = txtTitle.Text + fileExtension;

                // Suitable file location to store thumb.
                ImageUploader.PostedFile.SaveAs(path + Session["UploadImage"]);
                FileSaved = true;

            }
            if (FileSaved)
            {
                pnlUpload.Visible = false;
                pnlCropped.Visible = true;
                pnlCrop.Visible = true;
                imgOriginal.ImageUrl = "Images/" + Session["UploadImage"].ToString();
            }
        }

        /// <summary>
        /// Button click for Crop the selected area of image.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCrop_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(W.Value) || string.IsNullOrEmpty(H.Value) || string.IsNullOrEmpty(X.Value) || string.IsNullOrEmpty(Y.Value))
                return;
            // Get Image Original name.
            string imageName = Session["UploadImage"].ToString();
            // Get Selected Area
            int z = (int)Math.Ceiling(Convert.ToDouble(W.Value));
            int a = (int)Math.Ceiling(Convert.ToDouble(H.Value));
            int x = (int)Math.Ceiling(Convert.ToDouble(X.Value));
            int y = (int)Math.Ceiling(Convert.ToDouble(Y.Value));

            byte[] cropImage = Crop(path + imageName, z, a, x, y);
            // Save Cropped image to the server.
            using (MemoryStream ms = new MemoryStream(cropImage, 0, cropImage.Length))
            {
                ms.Write(cropImage, 0, cropImage.Length);
                using (SD.Image croppedImage = SD.Image.FromStream(ms, true))
                {
                    string croppedName = "cropped_" + imageName;
                    string saveTo = path + "cropped_" + imageName;
                    croppedImage.Save(saveTo, croppedImage.RawFormat);
                    imgOriginal.Visible = false;
                    pnlCropped.Visible = true;
                    btnReset.Visible = true;
                    btnCrop.Visible = false;
                    imgCropped.ImageUrl = "Images/" + croppedName;


                    // Save data to the database.
                    var imageModel = new ImageModel();
                    imageModel.OriginalImagePath = "Images/" + imageName;
                    imageModel.CroppedImagePath = "Images/" + croppedName;
                    imageModel.CroppedThumbnailPath = "Images/" + croppedName;
                    imageModel.ImageName = txtTitle.Text;
                    lblImageTitle.Text = "Title: " + txtTitle.Text;

                    // Insert uploaded image to the database.
                    var status = imageService.Insert(imageModel);
                }
            }
        }

        /// <summary>
        /// Get the cropped image from the selected image.
        /// </summary>
        /// <param name="Img"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <returns></returns>
        static byte[] Crop(string Img, int Width, int Height, int X, int Y)
        {
            try
            {
                using (SD.Image OriginalImage = SD.Image.FromFile(Img))
                {
                    using (SD.Bitmap bmp = new SD.Bitmap(Width, Height))
                    {
                        bmp.SetResolution(OriginalImage.HorizontalResolution, OriginalImage.VerticalResolution);
                        using (SD.Graphics Graphic = SD.Graphics.FromImage(bmp))
                        {
                            Graphic.SmoothingMode = SmoothingMode.AntiAlias;
                            Graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            Graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            Graphic.DrawImage(OriginalImage, new SD.Rectangle(0, 0, Width, Height), X, Y, Width, Height, SD.GraphicsUnit.Pixel);
                            MemoryStream ms = new MemoryStream();
                            bmp.Save(ms, OriginalImage.RawFormat);
                            return ms.GetBuffer();
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                throw (Ex);
            }
        }

        /// <summary>
        /// Reset page for next upload.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnReset_Click(object sender, EventArgs e)
        {
            imgOriginal.Visible = true;
            pnlUpload.Visible = true;
            btnCrop.Visible = true;
            txtTitle.Text = string.Empty;
            lblImageTitle.Text = string.Empty;
            W.Value = string.Empty;
            H.Value = string.Empty;
            X.Value = string.Empty;
            Y.Value = string.Empty;

            btnReset.Visible = false;
            pnlCropped.Visible = false;
            pnlCropped.Visible = false;
            pnlCrop.Visible = false;
            imgCropped.ImageUrl = string.Empty;
            imgOriginal.ImageUrl = string.Empty;
        }
    }
}