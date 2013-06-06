#ifndef _IPCSERVERCONNECTION_H
#define _IPCSERVERCONNECTION_H

#define ID_START         10000
#define ID_DISCONNECT    10001
#define ID_ADVISE         10002
#define ID_LOG          10003
#define ID_SERVERNAME    10004

// Define a new application
class IPCServer;
class MyFrame;

class IPCServerConnection : public wxConnection
{
public:
    IPCServerConnection(MyFrame *frame);
    ~IPCServerConnection();

    virtual bool OnExecute(const wxString& topic, wxChar *data, int size, wxIPCFormat format);
    virtual wxChar *OnRequest(const wxString& topic, const wxString& item, int *size, wxIPCFormat format);
    virtual bool OnPoke(const wxString& topic, const wxString& item, wxChar *data, int size, wxIPCFormat format);
    virtual bool OnStartAdvise(const wxString& topic, const wxString& item);
    virtual bool OnStopAdvise(const wxString& topic, const wxString& item);
    virtual bool Advise(const wxString& item, wxChar *data, int size = -1, wxIPCFormat format = wxIPC_TEXT);
    virtual bool OnDisconnect();
protected:
    void Log(const wxString& command, const wxString& topic, const wxString& item, wxChar *data, int size, wxIPCFormat format);
public:
    wxString        m_sAdvise;
protected:
    MyFrame         *callingFrame;
    wxString        m_sRequestDate;
    char             m_achRequestBytes[3];
};
#endif
