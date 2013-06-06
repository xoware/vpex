// ----------------------------------------------------------------------------
// IPCClient
// ----------------------------------------------------------------------------

#include <wx/wxprec.h>

#ifdef __BORLANDC__
    #pragma hdrstop
#endif

#ifndef WX_PRECOMP
    #include <wx/wx.h>
#endif

// Settings common to both executables: determines whether
// we're using TCP/IP or real DDE.
#include "ipcsetup.h"

#include <wx/datetime.h>
#include "IPCClient.h"
#include "IPCClientConnection.h"

IPCClient::IPCClient() : wxClient()
{
    m_connection = NULL;
}

bool IPCClient::Connect(const wxString& sHost, const wxString& sService, const wxString& sTopic)
{
    // suppress the log messages from MakeConnection()
    wxLogNull nolog;

    m_connection = (IPCClientConnection *)MakeConnection(sHost, sService, sTopic);
    return m_connection    != NULL;
}

wxConnectionBase *IPCClient::OnMakeConnection()
{
    return new IPCClientConnection;
}

void IPCClient::Disconnect()
{
    if (m_connection)
    {
        m_connection->Disconnect();
        delete m_connection;
        m_connection = NULL;
        //wxGetApp().GetFrame()->EnableControls();
        //wxLogMessage(_T("Client disconnected from server"));
    }
}

IPCClient::~IPCClient()
{
    Disconnect();
}
