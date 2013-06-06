#ifndef _VPEXCONNECTIONMANAGER_H
#define _VPEXCONNECTIONMANAGER_H

#include <wx/timer.h>
#include <wx/sound.h>
#include <wx/app.h>
#include <wx/snglinst.h>
#include <wx/frame.h>
#include <wx/button.h>
#include <wx/combobox.h>
#include "msgqueue.h"
#include "VPExProcess.h"

class MyFrame;
class LogWindowFrame;
class ManagementFrame;
class IPCClient;
class IPCServer;
class VPExExecutor;
class UnarchiveThread;

// set up timer
class UpdateTimer : public wxTimer
{
    MyFrame* frame;

public:
    UpdateTimer(MyFrame* frame);
    void Notify();
    void stop();
    void start();
private:
    wxSound *connectSound, *disconnectSound;
};

// Define the application class
class MyApp : public wxApp
{
public:
        IPCClient *m_client;
    // Init method
    virtual bool OnInit();
    virtual int OnExit();
	virtual void MacOpenFile(const wxString &fileName);
	void ProcessVpexFileIfNeeded(void);
	void HandleVpexFile(const wxString &fileName);
		
	UnarchiveThread *unarchiver;
	wxMessageQueue<wxString> *queue;
	
	void postMsgToQueue(wxString msg);
private:
	wxString fileToOpen;
    wxSingleInstanceChecker *m_checker;
	MyFrame *frame;
};

// Define a new dialog type: this is going to be our main window
class MyFrame : public wxFrame
{
public:
    // Constructor(s)
    MyFrame(const wxString& title, const wxPoint& pos, const wxSize& size);

    // Event handlers (these functions should _not_ be virtual)
    void OnQuitMenu(wxCommandEvent& event);
    void OnQuit(wxCloseEvent& event);
    void OnAbout(wxCommandEvent& event);
    void OnStartStop(wxCommandEvent& event);
    void OnLaunch(wxCommandEvent& event);
    void ShowHideLogWindow(wxCommandEvent& event);
    void ShowHideManagementWindow(wxCommandEvent& event);
	void SelectClient(wxCommandEvent& event);
	void HandleVpexFile(const wxString &fileName);
	void LaunchVpexConnection(const wxString &connectionName, const wxString &password);
	void TerminateVpexConnection(void);
	
	// event handlers
	//void OnTimer(wxTimerEvent& event);
    void OnIdle(wxIdleEvent& event);
	bool HandleProcessInput(void);
    
	// Set icon (from wxFrame source code)
    virtual void SetIcon(const wxIcon& icon);

	// misc utility funcs
	void enumerateVpexClients(void);
	void notifyOfNewConfiguration(void);
		
    // status display
    wxStatusBar *statusDisplay;
    wxButton *btnStartStop, *btnLaunch;
	wxComboBox *clientSelector;

    // log window
    LogWindowFrame *logWindowFrame;

	ManagementFrame *managementFrame;
	
    // connection status
    bool isConnected;

    bool readyToUnarchiveVpexFiles;

    wxMenu *viewMenuSave;

	IPCServer *m_server;
	
private:
    // confirm quit dialog
    bool ConfirmQuit(void);

    // executor
    VPExExecutor *executor;

    // update timer
    UpdateTimer *timer;

    // Frame icon
    wxIcon m_icon;

	// vpex process structure and pid
	VPExProcess *vpexProcess;
	long vpexProcessPid;

    // Any class wishing to process wxWindows events must use this macro
    DECLARE_EVENT_TABLE()
};


class UnarchiveThread : public wxThread
{
public:
    UnarchiveThread(MyFrame *frame, wxMessageQueue<wxString> *q);

    // thread execution starts here
    virtual void *Entry();

    // called when the thread exits - whether it terminates normally or is
    // stopped with Delete() (but not when it is Kill()ed!)
    virtual void OnExit();

private:
	MyFrame *frame;
    wxMessageQueue<wxString> *queue;
};

#endif
