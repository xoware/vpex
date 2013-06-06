// ----------------------------------------------------------------------------
// IPCServerConnection
// ----------------------------------------------------------------------------

// For compilers that support precompilation, includes "wx.h".
#include <wx/wxprec.h>
#include "VPExConnectionManager.h"

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

IPCServerConnection::IPCServerConnection(MyFrame *frame)
            : wxConnection()
{
   callingFrame = frame;
}

IPCServerConnection::~IPCServerConnection()
{
}

bool IPCServerConnection::OnExecute(const wxString& topic,
    wxChar *data, int size, wxIPCFormat format)
{
    Log(_T("OnExecute"), topic, _T(""), data, size, format);
    return true;
}

bool IPCServerConnection::OnPoke(const wxString& topic,
    const wxString& item, wxChar *data, int size, wxIPCFormat format)
{
    //wxLogMessage(_T("OnPoke"));

    Log(_T("OnPoke"), topic, item, data, size, format);
    callingFrame->enumerateVpexClients();
    return wxConnection::OnPoke(topic, item, data, size, format);
}

wxChar *IPCServerConnection::OnRequest(const wxString& topic,
    const wxString& item, int * size, wxIPCFormat format)
{
    wxChar *data;
    if (item == _T("Date"))
    {
        m_sRequestDate = wxDateTime::Now().Format();
        data = (wxChar *)m_sRequestDate.c_str();
        *size = -1;
    }    
    else if (item == _T("Date+len"))
    {
        m_sRequestDate = wxDateTime::Now().FormatTime() + _T(" ") + wxDateTime::Now().FormatDate();
        data = (wxChar *)m_sRequestDate.c_str();
        *size = (m_sRequestDate.Length() + 1) * sizeof(wxChar);
    }    
    else if (item == _T("bytes[3]"))
    {
        data = (wxChar *)m_achRequestBytes;
        m_achRequestBytes[0] = '1'; m_achRequestBytes[1] = '2'; m_achRequestBytes[2] = '3';
        *size = 3;
    }
    else
    {
        data = NULL;
        *size = 0;
    }
     Log(_T("OnRequest"), topic, item, data, *size, format);
    return data;
}

bool IPCServerConnection::OnStartAdvise(const wxString& topic,
                                 const wxString& item)
{
    //wxLogMessage(_T("OnStartAdvise(\"%s\",\"%s\")"), topic.c_str(), item.c_str());
    //wxLogMessage(_T("Returning true"));
    m_sAdvise = item;
    //wxGetApp().GetFrame()->Enable();
    return true;
}

bool IPCServerConnection::OnStopAdvise(const wxString& topic,
                                 const wxString& item)
{
    //wxLogMessage(_T("OnStopAdvise(\"%s\",\"%s\")"), topic.c_str(), item.c_str());
    //wxLogMessage(_T("Returning true"));
    m_sAdvise.Empty();
    //wxGetApp().GetFrame()->Enable();
    return true;
}

void IPCServerConnection::Log(const wxString& command, const wxString& topic,
    const wxString& item, wxChar *data, int size, wxIPCFormat format)
{
    wxString s;
    if (topic.IsEmpty() && item.IsEmpty())
        s.Printf(_T("%s("), command.c_str());
    else if (topic.IsEmpty())
        s.Printf(_T("%s(\"%s\","), command.c_str(), item.c_str());
    else if (item.IsEmpty())
        s.Printf(_T("%s(\"%s\","), command.c_str(), topic.c_str());
    else
        s.Printf(_T("%s(\"%s\",\"%s\","), command.c_str(), topic.c_str(), item.c_str());

    //if (format == wxIPC_TEXT || format == wxIPC_UNICODETEXT) 
        //wxLogMessage(_T("%s\"%s\",%d)"), s.c_str(), data, size);
    //else
    if (format == wxIPC_PRIVATE)
    {
        if (size == 3)
        {
            char *bytes = (char *)data;
            //wxLogMessage(_T("%s'%c%c%c',%d)"), s.c_str(), bytes[0], bytes[1], bytes[2], size);
        }
        //else
            //wxLogMessage(_T("%s...,%d)"), s.c_str(), size);
    }
    //else if (format == wxIPC_INVALID) 
        //wxLogMessage(_T("%s[invalid data],%d)"), s.c_str(), size);
}

bool IPCServerConnection::Advise(const wxString& item, wxChar *data, int size, wxIPCFormat format)
{
    Log(_T("Advise"), _T(""), item, data, size, format);
    return wxConnection::Advise(item, data, size, format);
}

bool IPCServerConnection::OnDisconnect()
{
    //wxLogMessage(_T("OnDisconnect()"));
    //wxGetApp().GetFrame()->Disconnect();
    return true;
}
