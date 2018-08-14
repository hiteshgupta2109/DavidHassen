Imports DavidHassen.DAL
Imports DavidHassen.Shared


''' <summary>
''' Service to perform Image related operations.
''' </summary>
Public Class ImageService
    Implements IImageService

#Region "Dependency Injection"

    Private imageProvider As IImageProvider

    Public Sub New()
        MyBase.New
        Me.imageProvider = New ImageProvider
    End Sub
#End Region

    Public Function Insert(ByVal imageModel As ImageModel) As Boolean Implements IImageService.Insert
        Return Me.imageProvider.Insert(imageModel)
    End Function

End Class
