Public Class ImageModel

    ''' <summary>
    ''' Default Initialization.
    ''' </summary>
    Public Sub New()
        MyBase.New
        ImageId = Guid.NewGuid
        CreatedDate = DateTime.UtcNow
        UpdateDate = DateTime.UtcNow
    End Sub

    Public Property ImageId As Guid
        Get
        End Get
        Set
        End Set
    End Property

    Public Property ImageName As String
        Get
        End Get
        Set
        End Set
    End Property

    Public Property OriginalImagePath As String
        Get
        End Get
        Set
        End Set
    End Property

    Public Property CroppedImagePath As String
        Get
        End Get
        Set
        End Set
    End Property

    Public Property CroppedThumbnailPath As String
        Get
        End Get
        Set
        End Set
    End Property

    Public Property CreatedDate As DateTime
        Get
        End Get
        Set
        End Set
    End Property

    Public Property UpdateDate As DateTime
        Get
        End Get
        Set
        End Set
    End Property
End Class