#ifndef _IPCSERVER_H
#define _IPCSERVER_H

#include "IPCServerConnection.h"

#define ID_START         10000
#define ID_DISCONNECT    10001
#define ID_ADVISE         10002
#define ID_LOG          10003
#define ID_SERVERNAME    10004

// Define a new application
class IPCServer;
class MyFrame;

class IPCServer: public wxServer
{
public:
    IPCServer(MyFrame* frame);
    ~IPCServer();
    void Disconnect();
    bool IsConnected() { return m_connection != NULL; };
    IPCServerConnection *GetConnection() { return m_connection; };
    void Advise();
    bool CanAdvise() { return m_connection != NULL && !m_connection->m_sAdvise.IsEmpty(); };
    wxConnectionBase *OnAcceptConnection(const wxString& topic);

protected:
    IPCServerConnection     *m_connection;
    MyFrame *callingFrame;
};
#endif
