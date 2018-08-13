using System;

namespace DavidHassen.Shared
{
    public class ImageModel
    {
        /// <summary>
        /// Default Initialization.
        /// </summary>
        public ImageModel()
        {
            ImageId = Guid.NewGuid();
            CreatedDate = DateTime.UtcNow;
            UpdateDate = DateTime.UtcNow;
        }

        public Guid ImageId { get; set; }
        public string ImageName { get; set; }
        public string OriginalImagePath { get; set; }
        public string CroppedImagePath { get; set; }
        public string CroppedThumbnailPath { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdateDate { get; set; }

    }
}
