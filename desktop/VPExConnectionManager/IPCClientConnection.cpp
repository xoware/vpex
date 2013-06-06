// ----------------------------------------------------------------------------
// IPCClientConnection
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

#include <wx/datetime.h>
#include "IPCClient.h"
#include "IPCClientConnection.h"

void IPCClientConnection::Log(const wxString& command, const wxString& topic,
    const wxString& item, wxChar *data, int size, wxIPCFormat format)
{
    wxString s;
    if (topic.IsEmpty() && item.IsEmpty())
        s.Printf(_T("%s("), command.c_str());
    else if (topic.IsEmpty())
        s.Printf(_T("%s(item=\"%s\","), command.c_str(), item.c_str());
    else if (item.IsEmpty())
        s.Printf(_T("%s(topic=\"%s\","), command.c_str(), topic.c_str());
    else
        s.Printf(_T("%s(topic=\"%s\",item=\"%s\","), command.c_str(), topic.c_str(), item.c_str());

    if (format == wxIPC_TEXT || format == wxIPC_UNICODETEXT)
	   int i = 42;	// no-op
        //wxLogMessage(_T("%s\"%s\",%d)"), s.c_str(), data, size);
    else if (format == wxIPC_PRIVATE)
    {
        if (size == 3)
        {
            char *bytes = (char *)data;
            //wxLogMessage(_T("%s'%c%c%c',%d)"), s.c_str(), bytes[0], bytes[1], bytes[2], size);
        }
        else
            //wxLogMessage(_T("%s...,%d)"), s.c_str(), size);
	    int i = 42;	// no-op
    }
    else if (format == wxIPC_INVALID)
	    int i = 42;	// no-op
        //wxLogMessage(_T("%s[invalid data],%d)"), s.c_str(), size);
}

bool IPCClientConnection::OnAdvise(const wxString& topic, const wxString& item, wxChar *data,
    int size, wxIPCFormat format)
{
    Log(_T("OnAdvise"), topic, item, data, size, format);
    return true;
}

bool IPCClientConnection::OnDisconnect()
{
    //wxLogMessage(_T("OnDisconnect()"));
    //wxGetApp().GetFrame()->Disconnect();
    return true;
}

bool IPCClientConnection::Execute(const wxChar *data, int size, wxIPCFormat format)
{
    Log(_T("Execute"), wxEmptyString, wxEmptyString, (wxChar *)data, size, format);
    bool retval = wxConnection::Execute(data, size, format);
    if (!retval)
        //wxLogMessage(_T("Execute failed!"));
    return retval;
}

wxChar *IPCClientConnection::Request(const wxString& item, int *size, wxIPCFormat format)
{
    wxChar *data =  wxConnection::Request(item, size, format);
    Log(_T("Request"), wxEmptyString, item, data, size ? *size : -1, format);
    return data;
}

bool IPCClientConnection::Poke(const wxString& item, wxChar *data, int size, wxIPCFormat format)
{
    Log(_T("Poke"), wxEmptyString, item, data, size, format);
    return wxConnection::Poke(item, data, size, format);
}
