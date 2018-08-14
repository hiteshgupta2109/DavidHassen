Imports DavidHassen.BLL
Imports DavidHassen.Shared
Imports System
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Web.UI
Imports SD = System.Drawing

Public Class _Default
    Inherits Page

#Region "Dependency Injection"

    Private imageService As IImageService

    ''' <summary>
    ''' Default constructor for resolve service dependency
    ''' </summary>
    Public Sub New()
        MyBase.New
        Me.imageService = New ImageService

    End Sub

    Private path As String = (System.Web.HttpContext.Current.Request.PhysicalApplicationPath + "Images\")
#End Region

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs)

    End Sub

    ''' <summary>
    ''' Button click event for Upload image to the server.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Protected Sub btnUpload_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim FileSaved = False
        ' Check if uploader has a file.
        If ImageUploader.HasFile Then
            Dim img = System.Drawing.Image.FromStream(ImageUploader.PostedFile.InputStream)
            Session("ImageHeight") = img.Height
            Session("ImageWidth") = img.Width
            'Session["UploadImage"] = ImageUploader.FileName;
            Dim fileExtension As String = System.IO.Path.GetExtension(ImageUploader.FileName).ToLower
            Session("UploadImage") = (txtTitle.Text + fileExtension)
            ' Suitable file location to store thumb.
            ImageUploader.PostedFile.SaveAs((Me.path + Session("UploadImage")))
            FileSaved = True
        End If

        If FileSaved Then
            pnlUpload.Visible = False
            pnlCrop.Visible = True
            imgOriginal.ImageUrl = ("Images/" + Session("UploadImage").ToString)
        End If

    End Sub

    ''' <summary>
    ''' Button click for Crop the selected area of image.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Protected Sub btnCrop_Click(ByVal sender As Object, ByVal e As EventArgs)
        If (String.IsNullOrEmpty(W.Value) _
                    OrElse (String.IsNullOrEmpty(H.Value) _
                    OrElse (String.IsNullOrEmpty(Me.X.Value) OrElse String.IsNullOrEmpty(Me.Y.Value)))) Then
            Return
        End If

        ' Get Image Original name.
        Dim imageName As String = Session("UploadImage").ToString
        ' Get Selected Area
        Dim z As Integer = CType(Math.Ceiling(Convert.ToDouble(W.Value)), Integer)
        Dim a As Integer = CType(Math.Ceiling(Convert.ToDouble(H.Value)), Integer)
        Dim x As Integer = CType(Math.Ceiling(Convert.ToDouble(Me.X.Value)), Integer)
        Dim y As Integer = CType(Math.Ceiling(Convert.ToDouble(Me.Y.Value)), Integer)
        Session("ImageSectionHeight") = ImageSectionHeight.Value
        Session("ImageSectionWidth") = ImageSectionWidth.Value
        Dim cropImage() As Byte = Me.Crop((Me.path + imageName), z, a, x, y)
        ' Save Cropped image to the server.
        Dim ms As MemoryStream = New MemoryStream(cropImage, 0, cropImage.Length)
        ms.Write(cropImage, 0, cropImage.Length)
        Dim croppedImage As SD.Image = SD.Image.FromStream(ms, True)
        Dim croppedName As String = ("cropped_" + imageName)
        Dim thumbName As String = ("thumb_" + imageName)
        Dim saveTo As String = (Me.path + croppedName)
        croppedImage.Save(saveTo, croppedImage.RawFormat)
        ' Save Image Thumb by 80x80
        Dim new_image As SD.Bitmap = New SD.Bitmap(80, 80)
        Dim g As SD.Graphics = SD.Graphics.FromImage(new_image)
        g.InterpolationMode = InterpolationMode.High
        g.DrawImage(croppedImage, 0, 0, 80, 80)
        saveTo = (Me.path + thumbName)
        new_image.Save(saveTo)
        imgOriginal.Visible = False
        pnlCropped.Visible = True
        btnReset.Visible = True
        btnCrop.Visible = False
        imgCropped.ImageUrl = ("Images/" + croppedName)
        imgThumb.ImageUrl = ("Images/" + thumbName)
        ' Save data to the database.
        Dim imageModel = New ImageModel
        imageModel.OriginalImagePath = ("Images/" + imageName)
        imageModel.CroppedImagePath = ("Images/" + croppedName)
        imageModel.CroppedThumbnailPath = ("Images/" + thumbName)
        imageModel.ImageName = txtTitle.Text
        lblImageTitle.Text = ("Title: " + txtTitle.Text)
        ' Insert uploaded image to the database.
        Dim status = Me.imageService.Insert(imageModel)
    End Sub

    ''' <summary>
    ''' Get the cropped image from the selected image.
    ''' </summary>
    ''' <param name="Img"></param>
    ''' <param name="Width"></param>
    ''' <param name="Height"></param>
    ''' <param name="X"></param>
    ''' <param name="Y"></param>
    ''' <returns></returns>
    Private Function Crop(ByVal Img As String, ByVal Width As Integer, ByVal Height As Integer, ByVal X As Integer, ByVal Y As Integer) As Byte()
        Try
            ' Calculate aspect ration for resized image
            Dim imageHeight = Convert.ToDouble(Session("ImageHeight").ToString)
            Dim imageWidth = Convert.ToDouble(Session("ImageWidth").ToString)
            Dim imageSectionHeight = Convert.ToDouble(Session("ImageSectionHeight").ToString)
            Dim imageSectionWidth = Convert.ToDouble(Session("ImageSectionWidth").ToString)
            Dim verticleRatio = (imageHeight / imageSectionHeight)
            Dim horizontalRatio = (imageWidth / imageSectionWidth)
            ' update parameter values
            Width = CType(Math.Ceiling((Width * horizontalRatio)), Integer)
            Height = CType(Math.Ceiling((Height * verticleRatio)), Integer)
            X = CType(Math.Ceiling((X * verticleRatio)), Integer)
            Y = CType(Math.Ceiling((Y * verticleRatio)), Integer)
            Dim OriginalImage As SD.Image = SD.Image.FromFile(Img)
            Dim bmp As SD.Bitmap = New SD.Bitmap(Width, Height)
            bmp.SetResolution(OriginalImage.HorizontalResolution, OriginalImage.VerticalResolution)
            Dim Graphic As SD.Graphics = SD.Graphics.FromImage(bmp)
            Graphic.SmoothingMode = SmoothingMode.AntiAlias
            Graphic.InterpolationMode = InterpolationMode.HighQualityBicubic
            Graphic.PixelOffsetMode = PixelOffsetMode.HighQuality
            Graphic.DrawImage(OriginalImage, New SD.Rectangle(0, 0, Width, Height), X, Y, Width, Height, SD.GraphicsUnit.Pixel)
            Dim ms As MemoryStream = New MemoryStream
            bmp.Save(ms, OriginalImage.RawFormat)
            Return ms.GetBuffer
        Catch Ex As Exception
            Throw
        End Try

    End Function

    ''' <summary>
    ''' Reset page for next upload.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Protected Sub btnReset_Click(ByVal sender As Object, ByVal e As EventArgs)
        imgOriginal.Visible = True
        pnlUpload.Visible = True
        btnCrop.Visible = True
        txtTitle.Text = String.Empty
        lblImageTitle.Text = String.Empty
        W.Value = String.Empty
        H.Value = String.Empty
        X.Value = String.Empty
        Y.Value = String.Empty
        Session("ImageHeight") = Nothing
        Session("ImageWidth") = Nothing
        Session("ImageSectionHeight") = Nothing
        Session("ImageSectionWidth") = Nothing
        Session("UploadImage") = Nothing
        btnReset.Visible = False
        pnlCropped.Visible = False
        pnlCrop.Visible = False
        imgCropped.ImageUrl = String.Empty
        imgOriginal.ImageUrl = String.Empty
    End Sub
End Class