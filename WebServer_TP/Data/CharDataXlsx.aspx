<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CharDataXlsx.aspx.cs" Inherits="JsWebServer_CP.Data.CharDataXlsx" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" 
	"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
<title>:: Importing Data From Excel Sheet to SQL Server ::</title>
<style type="text/css">
.style1
{
width: 50%;
}
</style>
</head>
<body>
<form id="form1" runat="server">
<div>

<table align="center" class="style1">
<tr>
<td align="center">
<asp:LinkButton ID="insertdata" runat="server" onclick="insertdata_Click">
	Insert Data</asp:LinkButton>
</td>
<td align="center">
<asp:LinkButton ID="viewdata" runat="server" onclick="viewdata_Click">
	View Data</asp:LinkButton>
</td>
<td align="center"> 
<asp:LinkButton ID="deletedata" runat="server" onclick="deletedata_Click">
	Delete Data</asp:LinkButton>
</td>
</tr>
<tr>
<td colspan="9" align="center">
<asp:GridView ID="GridView1" runat="server">
</asp:GridView>
</td>

</tr>
<tr>
<td colspan="9" align="center">
<asp:Label ID="lblmsg" runat="server" Width="500px"></asp:Label>
</td>

</tr>
</table>

</div>
</form>
</body>
</html> 