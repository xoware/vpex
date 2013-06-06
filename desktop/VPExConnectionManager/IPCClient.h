#ifndef _IPCCLIENT_H
#define _IPCCLIENT_H

#include <wx/ipc.h>
#include <wx/ipcbase.h>

#define ID_START         10000
#define ID_DISCONNECT    10001
#define ID_STARTADVISE    10002
#define ID_LOG          10003
#define ID_SERVERNAME    10004
#define ID_STOPADVISE    10005
#define ID_POKE            10006
#define ID_REQUEST        10007
#define ID_EXECUTE        10008
#define ID_TOPIC        10009
#define ID_HOSTNAME        10010

// Define a new application
class IPCClientConnection;

class IPCClient: public wxClient {
public:
    IPCClient();
    ~IPCClient();
    bool Connect(const wxString& sHost, const wxString& sService, const wxString& sTopic);
    void Disconnect();
    wxConnectionBase *OnMakeConnection();
    bool IsConnected() { return m_connection != NULL; };
    IPCClientConnection *GetConnection() { return m_connection; };

protected:
    IPCClientConnection     *m_connection;
};

#endif
