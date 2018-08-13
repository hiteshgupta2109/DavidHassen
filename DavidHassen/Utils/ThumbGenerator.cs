using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DavidHassen.Utils
{
    public class ThumbGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="filePath"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <param name="fixedWidthImage"></param>
        /// <returns></returns>
        public bool GenerateThumbnail(Stream inputStream, string filePath, int targetWidth, int targetHeight, bool cropImage, bool fixedWidthImage)
        {
            bool success = false;
            try
            {
                using (var image = Image.FromStream(inputStream))
                {

                    FileInfo file = new FileInfo(filePath);
                    var extension = file.Extension;

                    float targetRatio = targetWidth / (float)targetHeight;
                    int width = image.Width;
                    int height = image.Height;
                    float imageRatio = width / (float)height;

                    int newWidth;
                    int newHeight;
                    if (targetRatio > imageRatio && !fixedWidthImage)
                    {
                        newHeight = targetHeight;
                        newWidth = (int)Math.Floor(imageRatio * targetHeight);
                    }
                    else
                    {
                        newHeight = (int)Math.Floor(targetWidth / imageRatio);
                        newWidth = targetWidth;
                    }

                    //var thumbnailImg = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
                    if (cropImage)
                    {
                        using (var thumbnailImg = new Bitmap(targetWidth, targetHeight))
                        {
                            using (var graphics = Graphics.FromImage(thumbnailImg))
                            {
                                //var cropRect = new RectangleF(targetWidth, targetHeight, targetWidth, targetHeight);
                                ////graphics.DrawImage(image, new Rectangle(0, 0, targetWidth, targetHeight),
                                ////        cropRect,
                                ////        GraphicsUnit.Pixel);
                                //var newImage = thumbnailImg.Clone(cropRect, thumbnailImg.PixelFormat);


                                graphics.FillRectangle(new SolidBrush(Color.Black), new Rectangle(0, 0, newWidth, newHeight));
                                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.SmoothingMode = SmoothingMode.HighQuality;
                                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                graphics.DrawImage(image, 0, 0, targetWidth, targetHeight);


                                // Get an ImageCodecInfo object that represents the JPEG codec.
                                ImageCodecInfo imageCodecInfo1 = ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == image.RawFormat.Guid);

                                // Create an Encoder object for the Quality parameter.
                                Encoder encoder1 = Encoder.Quality;

                                // Create an EncoderParameters object. 
                                EncoderParameters encoderParameters1 = new EncoderParameters(1);

                                // Save the image as a JPEG file with quality level.
                                EncoderParameter encoderParameter1 = new EncoderParameter(encoder1, 95L);
                                encoderParameters1.Param[0] = encoderParameter1;

                                thumbnailImg.Save(filePath, imageCodecInfo1, encoderParameters1);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        using (var thumbnailImg = new Bitmap(newWidth, newHeight))
                        {
                            using (var graphics = Graphics.FromImage(thumbnailImg))
                            {

                                graphics.FillRectangle(new SolidBrush(Color.Black), new Rectangle(0, 0, newWidth, newHeight));
                                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                                graphics.CompositingQuality = CompositingQuality.HighQuality;
                                graphics.SmoothingMode = SmoothingMode.HighQuality;
                                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                                // Get an ImageCodecInfo object that represents the JPEG codec.
                                ImageCodecInfo imageCodecInfo = ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == image.RawFormat.Guid);

                                // Create an Encoder object for the Quality parameter.
                                Encoder encoder = Encoder.Quality;

                                // Create an EncoderParameters object. 
                                EncoderParameters encoderParameters = new EncoderParameters(1);

                                // Save the image as a JPEG file with quality level.
                                EncoderParameter encoderParameter = new EncoderParameter(encoder, 95L);
                                encoderParameters.Param[0] = encoderParameter;

                                // SAVE AFTER COMPRESS THE SIZE.
                                thumbnailImg.Save(filePath, imageCodecInfo, encoderParameters);
                                success = true;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }
        public bool GenerateFixedThumbnail(Stream inputStream, string filePath, int targetWidth, int targetHeight)
        {

            bool success;
            try
            {
                using (var image = Image.FromStream(inputStream))
                {
                    FileInfo file = new FileInfo(filePath);
                    var extension = file.Extension;


                    float targetRatio = targetWidth / (float)targetHeight;
                    int width = image.Width;
                    int height = image.Height;
                    float imageRatio = width / (float)height;

                    int newWidth;
                    int newHeight;
                    if (targetRatio > imageRatio)
                    {
                        newHeight = targetHeight;
                        newWidth = (int)Math.Floor(imageRatio * targetHeight);
                    }
                    else
                    {
                        newHeight = (int)Math.Floor(targetWidth / imageRatio);
                        newWidth = targetWidth;
                    }

                    using (Image thumbnailImg = new Bitmap(targetWidth, targetHeight))
                    {
                        using (Graphics graphics = Graphics.FromImage(thumbnailImg))
                        {
                            graphics.FillRectangle(new SolidBrush(Color.Black), new Rectangle(0, 0, newWidth, newHeight));
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            graphics.DrawImage(image, 0, 0, newWidth, newHeight);

                            //// SAVE WITHOUT COMPRESSION.
                            //thumbnailImg.Save(filePath, ImageFormat.Jpeg);

                            // Get an ImageCodecInfo object that represents the JPEG codec.
                            ImageCodecInfo imageCodecInfo = ImageCodecInfo.GetImageDecoders().SingleOrDefault(c => c.FormatID == image.RawFormat.Guid);

                            // Create an Encoder object for the Quality parameter.
                            Encoder encoder = Encoder.Quality;

                            // Create an EncoderParameters object. 
                            EncoderParameters encoderParameters = new EncoderParameters(1);

                            // Save the image as a JPEG file with quality level.
                            EncoderParameter encoderParameter = new EncoderParameter(encoder, 95L);
                            encoderParameters.Param[0] = encoderParameter;

                            // SAVE AFTER COMPRESS THE SIZE.
                            thumbnailImg.Save(filePath, imageCodecInfo, encoderParameters);

                            success = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public bool GenerateThumbnail(Image originalImage, string filePath, int targetWidth, int targetHeight, bool fixedWidthImage)
        {

            FileInfo file = new FileInfo(filePath);
            var extension = file.Extension;

            bool success;
            float targetRatio = targetWidth / (float)targetHeight;
            int width = originalImage.Width;
            int height = originalImage.Height;
            float imageRatio = width / (float)height;

            int newWidth;
            int newHeight;
            if (targetRatio > imageRatio && !fixedWidthImage)
            {
                newHeight = targetHeight;
                newWidth = (int)Math.Floor(imageRatio * targetHeight);
            }
            else
            {
                newHeight = (int)Math.Floor(targetWidth / imageRatio);
                newWidth = targetWidth;
            }

            //newWidth = newWidth > targetWidth ? targetWidth : newWidth;
            //newHeight = newHeight > targetHeight ? targetHeight : newHeight;
            try
            {
                using (Image finalImage = new Bitmap(newWidth, newHeight))
                {
                    using (Graphics graphics = Graphics.FromImage(finalImage))
                    {
                        graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, newWidth, newHeight));
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;

                        finalImage.Save(filePath, originalImage.RawFormat);

                        finalImage.Dispose();
                        success = true;
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }
        public bool GenerateFixedThumbnail(Image originalImage, string filePath, int targetWidth, int targetHeight)
        {

            FileInfo file = new FileInfo(filePath);
            var extension = file.Extension;

            bool success;
            float targetRatio = targetWidth / (float)targetHeight;
            int width = originalImage.Width;
            int height = originalImage.Height;
            float imageRatio = width / (float)height;

            int newWidth;
            int newHeight;
            if (targetRatio > imageRatio)
            {
                newHeight = targetHeight;
                newWidth = (int)Math.Floor(imageRatio * targetHeight);
            }
            else
            {
                newHeight = (int)Math.Floor(targetWidth / imageRatio);
                newWidth = targetWidth;
            }

            //newWidth = newWidth > targetWidth ? targetWidth : newWidth;
            //newHeight = newHeight > targetHeight ? targetHeight : newHeight;
            try
            {
                using (Image finalImage = new Bitmap(targetWidth, targetHeight))
                {
                    using (Graphics graphics = Graphics.FromImage(finalImage))
                    {
                        graphics.FillRectangle(new SolidBrush(Color.Transparent), new Rectangle(0, 0, newWidth, newHeight));
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;

                        finalImage.Save(filePath, originalImage.RawFormat);

                        finalImage.Dispose();
                        success = true;
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static Image PadImage(Image originalImage)
        {
            int largestDimension = Math.Max(originalImage.Height, originalImage.Width);
            var squareSize = new Size(largestDimension, largestDimension);
            var squareImage = new Bitmap(squareSize.Width, squareSize.Height);
            using (Graphics graphics = Graphics.FromImage(squareImage))
            {
                graphics.FillRectangle(Brushes.Transparent, 0, 0, squareSize.Width, squareSize.Height);
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighSpeed;

                graphics.DrawImage(originalImage, (squareSize.Width / 2) - (originalImage.Width / 2), (squareSize.Height / 2) - (originalImage.Height / 2), originalImage.Width, originalImage.Height);
            }
            return squareImage;
        }

        private static ImageCodecInfo GetEncoderInfo(Guid guid)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].FormatID == guid)
                    return encoders[j];
            }
            return null;
        }
    }

}