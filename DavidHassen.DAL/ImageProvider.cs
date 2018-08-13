using DavidHassen.Shared;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DavidHassen.DAL
{
    public class ImageProvider: IImageProvider
    {

        /// <summary>
        /// Insert ImageLibrary information.
        /// </summary>
        /// <remarks>
        /// <param name="imageModel">Passing all ImageLibrary info."</param> 
        /// <returns>returning ImageLibraryId which is generated.</returns>
        /// </remarks>
        public bool Insert(ImageModel imageModel)
        {
            var conn = new SqlConnection(SqlHelper.GetConnectionString());
            SqlTransaction sqlTrans = null;
            var param = new List<SqlParameter>();
            try
            {
                conn.Open();
                sqlTrans = conn.BeginTransaction();
                /*Add the Parameters*/

                param.Add(new SqlParameter { ParameterName = "@ImageId", Value = imageModel.ImageId });
                param.Add(new SqlParameter { ParameterName = "@ImageName", Value = imageModel.ImageName });
                param.Add(new SqlParameter { ParameterName = "@CroppedImagePath", Value = imageModel.CroppedImagePath });
                param.Add(new SqlParameter { ParameterName = "@CroppedThumbnailPath", Value = imageModel.CroppedThumbnailPath });
                param.Add(new SqlParameter { ParameterName = "@OriginalImagePath", Value = imageModel.OriginalImagePath });
                param.Add(new SqlParameter { ParameterName = "@CreatedDate", Value = imageModel.CreatedDate });
                param.Add(new SqlParameter { ParameterName = "@UpdateDate", Value = imageModel.UpdateDate });

                SqlHelper.ExecuteScalar(sqlTrans, CommandType.StoredProcedure, "spInsertImage", param.ToArray());
                //Commit transaction for saving info
                sqlTrans.Commit();

                if (conn.State == ConnectionState.Open)
                    conn.Close();
                return true;
            }
            catch  //Error...
            {
                if (sqlTrans != null)
                    if (sqlTrans.Connection.State == ConnectionState.Open)
                    {
                        //rollback transaction 
                        sqlTrans.Rollback();
                        conn.Close();
                    }
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    //closing connection
                    conn.Close();
                }
            }
            return false;
        }


    }
}
