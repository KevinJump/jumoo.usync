<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncTab.ascx.cs" Inherits="jumoo.usync.usyncui.uSyncTab" %>
<div class="propertypane">
    <div class="propertyItem">
        <div class="dashboardWrapper">
            <h2>uSync (Content Edition)</h2>
            <img src="./dashboard/images/starterkit32x32.png" alt="uSync" class="dashboardIcon">
            <p>
                uSync.ContentEdition syncs content & media to and from disk. the default configuration is to 
                do this when the site starts and at everysave. so you shouldn't need the buttons below.
            </p>
            <h3>Full Import</h3>
            <p>
                <asp:Button ID="fullImport" runat="server" Text="Full Import" OnClick="fullImport_Click" CssClass="usyncBtn" />
                a full import happens, when the application starts, click the link above to do it now.
            </p>
            <h3>Full Export</h3>
            <p>
                <asp:Button ID="fullExport" runat="server" Text="Full Export" OnClick="fullExport_Click" CssClass="usyncBtn" />
                Export all content and media to disk, <strong>this will overwrite any previous syncs you have on disk</strong>
            </p>
            <p>
                <div style="border-top: 2px solid #888;">
                    <asp:Label ID="lblStatus" runat="server" style="color: #800; padding: 1em; font-size:1.6em;display:block;"/>
                </div>
            </p>
        </div>
    </div>
</div>

