﻿<%@ Page Title="Home Page" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.vb" Inherits="DavidHassen._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <script>
        jQuery(window).load(function () {
            var jcrop_api;
            var i, ac;
            var cropSize = 0;

            initJcrop();

            function initJcrop() {
                jcrop_api = jQuery.Jcrop('#<%= imgOriginal.ClientID %>', {
                    onSelect: storeCoords,
                    onChange: storeCoords
                });
                //jcrop_api.setOptions({ aspectRatio: 16/ 9 });
                jcrop_api.setOptions({
                    minSize: [80, 80],
                    maxSize: [800, 200]
                });
                jcrop_api.setSelect([0, 0, 80, 80]);
            };
            function resetCropSize() {
                jcrop_api.setOptions({
                    minSize: [80, 80],
                    maxSize: [800, 200]
                });
            }
            function storeCoords(c) {
                if (cropSize == 0) {
                    var width = jQuery('#imgOriginal').width();
                    var height = jQuery('#imgOriginal').height();
                    cropSize = width > height ? height : width;
                    jcrop_api.setOptions({
                        minSize: [cropSize, cropSize],
                        maxSize: [cropSize, cropSize]
                    });
                }
                jQuery('#X').val(c.x);
                jQuery('#Y').val(c.y);
                jQuery('#W').val(c.w);
                jQuery('#H').val(c.h);
                jQuery('#ImageSectionWidth').val(jQuery('#imgOriginal').width());
                jQuery('#ImageSectionHeight').val(jQuery('#imgOriginal').height());

            };
        });

        function validateImageExtension() {
            var title = $("#txtTitle");
            if (title.val() == "") {
                $("#Label1").text("Please provide an image title.");
                return false;
            }
            $("#Label1").text("");
            var allowedFiles = [".png", ".jpg", ".gif", ".jpeg"];
            var fileUpload = $("#ImageUploader");
            var lblError = $("#lblUploaderMessage");
            var regex = new RegExp("([a-zA-Z0-9\(\)\s_\\.\-:])+(" + allowedFiles.join('|') + ")$");
            if (!regex.test(fileUpload.val().toLowerCase())) {
                $("#lblUploaderMessage").text("Please upload files having extensions: " + allowedFiles.join(', ') + " only.");
                return false;
            }

            $("#lblUploaderMessage").text("");
            return true;
        }

    </script>
    <div class="jumbotron">
        <div class="row">
            <asp:Panel ID="pnlUpload" runat="server">
                <div class="col-md-6">
                    <asp:Label runat="server" class="font-bold" Text="Title:"></asp:Label>
                    <asp:TextBox runat="server" ClientIDMode="Static" ID="txtTitle" class="form-control" MaxLength="20"></asp:TextBox>
                    <asp:Label ID="Label1" runat="server" Text="" ClientIDMode="Static" class="error-text"></asp:Label>
                </div>
                <div class="col-md-6">
                    <asp:Label runat="server" class="font-bold" Text="Upload Image:"></asp:Label>
                    <asp:FileUpload ID="ImageUploader" runat="server" ClientIDMode="Static" padding-bottom="4" AllowMultiple="false" ToolTip="Click to select Image" />
                    <asp:Button ID="btnImageUpload" runat="server" Text="Upload" ToolTip="Upload Image" CssClass="small-button btn btn-primary" ClientIDMode="Static" UseSubmitBehavior="true"
                        OnClick="btnUpload_Click" OnClientClick="return validateImageExtension();" />
                    <asp:Label ID="lblUploaderMessage" runat="server" Text="" ClientIDMode="Static" class="error-text"></asp:Label>
                </div>
            </asp:Panel>
        </div>

        <asp:Panel ID="pnlCropped" runat="server" Visible="false">
            <div class="row m-t20">
                <div class="col-md-3">
                    <asp:Label runat="server" ID="lblImageTitle" class="font-bold"></asp:Label>
                </div>
                <div class="col-md-3">
                    <asp:Button ID="btnReset" runat="server" CssClass="small-button btn btn-primary" Text="Upload Again" Visible="false" OnClick="btnReset_Click" />
                </div>
            </div>
            <div class="row mt40">
                <div class="col-md-12">
                    <label class="font-bold">Cropped Image:</label><br />
                    <asp:Image ID="imgCropped" runat="server" />
                </div>
            </div>
            <div class="row">
                <div class="col-md-12">
                    <label class="font-bold">Thumb Image:</label><br />
                    <asp:Image ID="imgThumb" runat="server" />
                </div>
            </div>
        </asp:Panel>
        <div class="row">
            <asp:Panel ID="pnlCrop" runat="server" Visible="false">

                <div class="col-md-12">
                    <asp:Button ID="btnCrop" runat="server" CssClass="small-button btn btn-primary" Text="Crop" OnClick="btnCrop_Click" />
                </div>
                <div class="col-md-12 m-t20">
                    <asp:Image ID="imgOriginal" runat="server" class="img-crop" ClientIDMode="Static" />
                    <asp:HiddenField ID="X" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="Y" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="W" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="H" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="ImageSectionWidth" runat="server" ClientIDMode="Static" />
                    <asp:HiddenField ID="ImageSectionHeight" runat="server" ClientIDMode="Static" />
                </div>
            </asp:Panel>
        </div>

    </div>
</asp:Content>
