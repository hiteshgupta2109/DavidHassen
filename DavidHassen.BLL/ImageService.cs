using DavidHassen.DAL;
using DavidHassen.Shared;

namespace DavidHassen.BLL
{
    /// <summary>
    /// Service to perform Image related operations.
    /// </summary>
    public class ImageService: IImageService
    {
        #region Dependency Injection

        IImageProvider imageProvider;
        public ImageService() {
            imageProvider = new ImageProvider();
        }
        #endregion

        /// <summary>
        /// Insert new record for the uploaded image.
        /// </summary>
        /// <param name="imageModel"></param>
        /// <returns></returns>
        public bool Insert(ImageModel imageModel)
        {
            return imageProvider.Insert(imageModel);
        }

    }
}
