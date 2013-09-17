#ifndef _IPCCLIENTCONNECTION_H
#define _IPCCLIENTCONNECTION_H

class IPCClientConnection: public wxConnection
{
public:
    virtual bool Execute(const wxChar *data, int size = -1, wxIPCFormat format = wxIPC_TEXT);
    virtual wxChar *Request(const wxString& item, int *size = NULL, wxIPCFormat format = wxIPC_TEXT);
    virtual bool Poke(const wxString& item, wxChar *data, int size = -1, wxIPCFormat format = wxIPC_TEXT);
    virtual bool OnAdvise(const wxString& topic, const wxString& item, wxChar *data, int size, wxIPCFormat format);
    virtual bool OnDisconnect();
protected:
    void Log(const wxString& command, const wxString& topic,
        const wxString& item, wxChar *data, int size, wxIPCFormat format);
};

#endif