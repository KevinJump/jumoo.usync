<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="uSyncTab.ascx.cs" Inherits="jumoo.usync.usyncui.uSyncTab" %>
<div class="propertypane">
    <div class="propertyItem">
        <div class="dashboardWrapper">
            <h2>uSync (Content Edition)</h2>
            <img src="./dashboard/images/starterkit32x32.png" alt="uSync" class="dashboardIcon">
            <p>
                uSync Content Edition is an experimental content export and import tool, designed to help you 
                move content from one umbraco install to another (mainly for developers to make a snapshot). 
            </p>
            <p>
                uSync Content Edition only does the content, it assumes you've already synced things like doctypes,
                macros and templates - (if you need help with that try 
                <a href="http://our.umbraco.org/projects/developer-tools/usync" target="_new">uSync</a>)
            </p>
        </div>
    </div>
</div>

<div class="propertypane">
    <div class="propertyItem">
        <div class="dashboardWrapper">
            <h2>Export Content</h2>
            <img src="./dashboard/images/starterkit32x32.png" alt="uSync" class="dashboardIcon">
            <p>
                Go through all the content in this site and write it to disk (look in /uSync.Content)
            </p>
            <asp:Button ID="exportContent" runat="server" Text="export content" OnClick="exportContent_Click" />

            <div>
                <asp:Label ID="exportStatus" runat="server"></asp:Label>
            </div>
        </div>
    </div>
</div>

<div class="propertypane">
    <div class="propertyItem">
        <div class="dashboardWrapper">
            <h2>Import Content</h2>
            <img src="./dashboard/images/starterkit32x32.png" alt="uSync" class="dashboardIcon">
            <p>
                Read the content in the uSync.Content folder and create/update the content on this site.
            </p>
            <asp:Button ID="importContent" runat="server" Text="Import content" OnClick="importContent_Click" />

            <div>
                <asp:Label ID="importStatus" runat="server"></asp:Label>
            </div>
        </div>
    </div>
</div>
