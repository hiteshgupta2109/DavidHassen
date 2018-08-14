Imports System.Data.SqlClient
Imports DavidHassen.Shared

Public Class ImageProvider
    Implements IImageProvider

    ''' <summary>
    ''' Insert ImageLibrary information.
    ''' </summary>
    ''' <remarks>
    ''' <param name="imageModel">Passing all ImageLibrary info."</param> 
    ''' <returns>returning ImageLibraryId which is generated.</returns>
    ''' </remarks>
    Public Function Insert(ByVal imageModel As ImageModel) As Boolean Implements IImageProvider.Insert
        Dim conn = New SqlConnection(SqlHelper.GetConnectionString)
        Dim sqlTrans As SqlTransaction = Nothing
        Dim param = New List(Of SqlParameter)
        Try
            conn.Open()
            sqlTrans = conn.BeginTransaction

            Dim n = New SqlParameter
            n.ParameterName = "@ImageName"
            n.Value = imageModel.ImageName
            param.Add(n)

            n = New SqlParameter
            n.ParameterName = "@CroppedImagePath"
            n.Value = imageModel.CroppedImagePath
            param.Add(n)

            n = New SqlParameter
            n.ParameterName = "@CroppedThumbnailPath"
            n.Value = imageModel.CroppedThumbnailPath
            param.Add(n)

            n = New SqlParameter
            n.ParameterName = "@OriginalImagePath"
            n.Value = imageModel.OriginalImagePath
            param.Add(n)

            n = New SqlParameter
            n.ParameterName = "@CreatedDate"
            n.Value = imageModel.CreatedDate
            param.Add(n)

            n = New SqlParameter
            n.ParameterName = "@UpdateDate"
            n.Value = imageModel.UpdateDate
            param.Add(n)

            SqlHelper.ExecuteScalar(sqlTrans, CommandType.StoredProcedure, "spInsertImage", param.ToArray)
            'Commit transaction for saving info
            sqlTrans.Commit()

            If (conn.State = ConnectionState.Open) Then
                conn.Close()
            End If

            Return True
        Catch ex As System.Exception
            If (Not (sqlTrans) Is Nothing) Then
                If (sqlTrans.Connection.State = ConnectionState.Open) Then
                    'rollback transaction 
                    sqlTrans.Rollback()
                    conn.Close()
                End If

            End If

        Finally
            If (conn.State = ConnectionState.Open) Then
                'closing connection
                conn.Close()
            End If

        End Try

        Return False
    End Function

End Class
