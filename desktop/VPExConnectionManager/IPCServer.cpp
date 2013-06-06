// ----------------------------------------------------------------------------
// IPCServer
// ----------------------------------------------------------------------------

// For compilers that support precompilation, includes "wx.h".
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

#include "IPCServer.h"
#include <wx/textdlg.h>
#include <wx/datetime.h>

IPCServer::IPCServer(MyFrame* frame) : wxServer()
{
    callingFrame = frame;
    m_connection = NULL;
}

IPCServer::~IPCServer()
{
    Disconnect();
}

wxConnectionBase *IPCServer::OnAcceptConnection(const wxString& topic)
{
    //wxLogMessage(_T("OnAcceptConnection(\"%s\")"), topic.c_str());

    if ( topic == IPC_TOPIC )
    {
        m_connection = new IPCServerConnection(callingFrame);
        //wxGetApp().GetFrame()->Enable();
        //wxLogMessage(_T("Connection accepted"));
        return m_connection;
    }
    // unknown topic
    return NULL;
}

void IPCServer::Disconnect()
{
    if (m_connection)
    {
        m_connection->Disconnect();
        delete m_connection;
        m_connection = NULL;
        //wxGetApp().GetFrame()->Enable();
        //wxLogMessage(_T("Disconnected client"));
    }
}

void IPCServer::Advise()
{
    if (CanAdvise())
    {
        wxString s = wxDateTime::Now().Format();
        m_connection->Advise(m_connection->m_sAdvise, (wxChar *)s.c_str());
        s = wxDateTime::Now().FormatTime() + _T(" ") + wxDateTime::Now().FormatDate();
        m_connection->Advise(m_connection->m_sAdvise, (wxChar *)s.c_str(), (s.Length() + 1) * sizeof(wxChar));

#if wxUSE_DDE_FOR_IPC
        //wxLogMessage(_T("DDE Advise type argument cannot be wxIPC_PRIVATE. The client will receive it as wxIPC_TEXT, and receive the correct no of bytes, but not print a correct log entry."));
#endif
        char bytes[3];
        bytes[0] = '1'; bytes[1] = '2'; bytes[2] = '3';
        m_connection->Advise(m_connection->m_sAdvise, (wxChar *)bytes, 3, wxIPC_PRIVATE);
        // this works, but the log treats it as a string now
//        m_connection->Advise(m_connection->m_sAdvise, (wxChar *)bytes, 3, wxIPC_TEXT );
    }
}
