// ============================================================================
// VPEx Connection Manager
// Donald Burr, VCT Labs
// February 2012
// ============================================================================

// ============================================================================
// declarations
// ============================================================================

// ----------------------------------------------------------------------------
// headers
// ----------------------------------------------------------------------------

#ifdef __GNUG__
#pragma implementation "dlgapp.cpp"
#pragma interface "dlgapp.cpp"
#endif

// For compilers that support precompilation, includes "wx/wx.h".
#include "wx/wxprec.h"

#ifdef __BORLANDC__
#pragma hdrstop
#endif

// For all others, include the necessary headers (this file is usually all you
// need because it includes almost all "standard" wxWindows headers
#ifndef WX_PRECOMP
#include "wx/wx.h"
#endif

// ----------------------------------------------------------------------------
// ressources
// ----------------------------------------------------------------------------
// The application icon
// Note: if __WIN95__ is defined the application icon appears in the
//       dialog title bar and on the taskbar (loaded from resources)
#if defined(__WXGTK__) || defined(__WXMOTIF__)
#include "vpex.xpm"
#endif

#include <iostream>
#include <wx/splash.h>
#include <wx/process.h>
#include <wx/timer.h>
#include <wx/snglinst.h>
#include <wx/sound.h>
#include <wx/stdpaths.h>
#include <wx/log.h>
#include <wx/combobox.h>
#include <wx/dir.h>
#include <wx/wfstream.h>
#include <wx/zipstrm.h>
#include <wx/textfile.h>
#include <wx/filefn.h>
#include <wx/stdpaths.h>

#if defined(__WINDOWS__) || defined(__WIN32__) || defined(_WIN64)  || defined(_WIN32)
	#define PATHSEP _("\\")
#elif defined (__WXMAC__) || defined (__APPLE_)
	#include <ApplicationServices/ApplicationServices.h>
	#define PATHSEP _("/")
#elif defined(__unix)
	#define PATHSEP _("/")
#else
	#error "OS not detected"
#endif

#include "LogWindowFrame.h"
#include "ManagementFrame.h"
#include "FindProcByName.h"
#include "NetUtils.h"
#include "VPExConnectionManager.h"
#include "IPCClient.h"

#include "IPCServer.h"
//#include "IPCServerConnection.h"
//#include "ipcsetup.h"

#include "version.h"

inline void setControlEnable(int id, bool state)
{
	wxWindow *win = wxWindow::FindWindowById(id);
	if(win) win->Enable(state);
}

static bool readyToUnarchiveVpexFiles;

// ----------------------------------------------------------------------------
// constants
// ----------------------------------------------------------------------------

// IDs for the controls in the dialog
enum
{
    // command buttons
    Exec_StartStop = 1,
    Exec_Launch,
	Exec_ClientSelected,
    Exec_About,
    Exec_ShowHideLogWindow,
	Exec_ShowHideManagementWindow,
    Exec_Close
};

// ----------------------------------------------------------------------------
// event tables and other macros for wxWindows
// ----------------------------------------------------------------------------

BEGIN_EVENT_TABLE(MyFrame, wxFrame)
    EVT_BUTTON(Exec_StartStop, MyFrame::OnStartStop)
    EVT_BUTTON(Exec_Launch, MyFrame::OnLaunch)
	EVT_COMBOBOX(Exec_ClientSelected, MyFrame::SelectClient)
    EVT_MENU(Exec_ShowHideLogWindow, MyFrame::ShowHideLogWindow)
	EVT_MENU(Exec_ShowHideManagementWindow, MyFrame::ShowHideManagementWindow)
#ifdef __WXMAC__
    EVT_MENU(wxID_ABOUT, MyFrame::OnAbout)
    EVT_MENU(wxID_EXIT, MyFrame::OnQuitMenu)
#else
    EVT_MENU(Exec_About, MyFrame::OnAbout)
    EVT_MENU(Exec_Close, MyFrame::OnQuitMenu)
#endif
    EVT_IDLE(MyFrame::OnIdle)
    // We have to implement this to force closing
    // the dialog when the 'x' button is pressed
    EVT_CLOSE(MyFrame::OnQuit)
END_EVENT_TABLE()

IMPLEMENT_APP(MyApp)

// ============================================================================
// implementation
// ============================================================================

// ----------------------------------------------------------------------------
// the application class
// ----------------------------------------------------------------------------

bool MyApp::OnInit()
{
	printf("Startup.. Compiled "  __DATE__ " " __TIME__ "\n");
	wxLogDebug("Staring up.   Compiled " __DATE__ " " __TIME__);

/*
#ifdef __WXMAC__
	ProcessSerialNumber PSN;
	GetCurrentProcess(&PSN);
	TransformProcessType(&PSN,kProcessTransformToForegroundApplication);
#endif
*/

    const wxString name = wxString::Format(_T(".VPExConnectionManager.lock.%s"), wxGetUserId().c_str());
    m_checker = new wxSingleInstanceChecker(name);

    // handle opened files

    if (argc > 1) {
		wxString filename(argv[1]);
		HandleVpexFile(filename);
	} else {
		// make sure only a single instance is running
		if (m_checker->IsAnotherRunning())  {
			wxString errorMsg = _T("Another instance of this program is already running.");
			wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
			dialog->ShowModal();
			// brutal hack
			exit(0);
		}
	}

    // show splash screen
    wxInitAllImageHandlers();
    wxBitmap bitmap;
    wxString splashPath = wxStandardPaths::Get().GetResourcesDir() << PATHSEP << _("splash.png");
    wxSplashScreen* splash = NULL;
    if (bitmap.LoadFile(splashPath, wxBITMAP_TYPE_PNG))
    {
        splash = new wxSplashScreen(bitmap,
                wxSPLASH_CENTRE_ON_SCREEN|wxSPLASH_TIMEOUT,
                2000, NULL, -1, wxDefaultPosition, wxDefaultSize,
                wxFRAME_NO_TASKBAR|wxSIMPLE_BORDER|wxSTAY_ON_TOP);
        // wxSIMPLE_BORDER wxDOUBLE_BORDER
    }
    //wxYield();
    wxSleep(2);

    // Create the main application window (a dialog in this case)
    // NOTE: Vertical dimension comprises the caption bar.
    //       Horizontal dimension has to take into account the thin
    //       hilighting border around the dialog (2 points in
    //       Win 95).
    frame = new MyFrame(_T("VPEx Connection Manager"),
				wxDefaultPosition, wxSize(300, 300));

    // Show it and tell the application that it's our main window
    frame->SetBackgroundColour( wxColor( 255,255,255 ));
    frame->Show(TRUE);
    SetTopWindow(frame);

	// set up unarchive thread
	queue = new wxMessageQueue<wxString>();
	unarchiver = new UnarchiveThread(frame, queue);
	unarchiver->Create();
	unarchiver->Run();

    if (fileToOpen && wxFileExists(fileToOpen))  {
		postMsgToQueue(fileToOpen);
    }

	readyToUnarchiveVpexFiles = true;

    return TRUE;
}

void MyApp::postMsgToQueue(wxString msg)
{
	queue->Post(msg);
}

int MyApp::OnExit()
{
    delete m_checker;
    return 0;
}

void MyApp::MacOpenFile(const wxString &fileName)
{
	fileToOpen = fileName;
	
	if (readyToUnarchiveVpexFiles)  {
		postMsgToQueue(fileName);
	}
}

void MyApp::ProcessVpexFileIfNeeded(void)
{
    if (fileToOpen && wxFileExists(fileToOpen))  {
	    frame->HandleVpexFile(fileToOpen);
    }
}

void MyApp::HandleVpexFile(const wxString &fileName)
{
	/*
    const wxString name = wxString::Format(_T(".VPExConnectionManager.lock.%s"), wxGetUserId().c_str());
    wxSingleInstanceChecker *m_checker = new wxSingleInstanceChecker(name);
	*/
	
	wxYield();

	// create vpex config dir if it doesn't exist
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	if (!wxDir::Exists(configPath))  {
		// directory does not exist, let's make it
		if (!::wxMkdir(configPath))  {
			wxYield();
			wxString errorMsg = _T("Could not create VPEx configuration directory at ");
			errorMsg << configPath << _T(".");
			wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
			wxYield();
			dialog->ShowModal();
			exit(0);
		}
	}

	wxFileInputStream* file = new wxFileInputStream(fileName);
	wxZipInputStream* archiveFile = new wxZipInputStream(file);
	if ( archiveFile->IsOk() && archiveFile->CanRead() && (file->GetSize() != 0) ) {
		wxZipEntry* archiveFileMetaData;
	   	while ( (archiveFileMetaData = archiveFile->GetNextEntry()) != NULL ) {
			wxYield();
			wxString unzippedFileName = archiveFileMetaData->GetInternalName(); // gets name of the zipped file
	   		wxFileOutputStream unzippedFile(wxStandardPaths::Get().GetUserDataDir() + PATHSEP + unzippedFileName);
			archiveFile->Read(unzippedFile);
	   		unzippedFile.Close();
		}
		archiveFile->CloseEntry();
	}

	// attempt a connection
	wxYield();
	m_client = new IPCClient;
    bool retval = m_client->Connect(_("localhost"), _("4242"), _("VPEx"));

    if (retval)  {
		wxString errorMsg = _T("New VPEx profile installed.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();

        wxString s = wxDateTime::Now().Format();
        m_client->GetConnection()->Poke(_T("Date"), (wxChar *)s.c_str());
	} else {
		wxString errorMsg = _T("New VPEx configuration installed.  It will be available the next time you launch VPEx Connection Manager.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
		// XXX brutal hack
    }
    delete m_client;
    m_client = NULL;
	exit(0);
}

// ----------------------------------------------------------------------------
// main dialog
// ----------------------------------------------------------------------------

// Frame constructor
MyFrame::MyFrame(const wxString& title, const wxPoint& pos, const wxSize& size)
        : wxFrame((wxFrame *)NULL, -1, title, pos, size)
{
	// set sane default value for pid
	vpexProcessPid = 0;

    // start out in disconnected state
    isConnected = false;

	// set up the server
	// bool retval = m_client->Connect(_("localhost"), _("4242"), _("VPEx"));

	wxYield();

   	m_server = new IPCServer(this);
    if (!m_server->Create(_T("4242")))  {
	wxYield();
	wxString errorMsg = _T("Could not start VPEx configuration change notification service. You will need to quit and relaunch VPEx Connection Manager when adding or deleting VPEx connections.");
	wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
	int response = dialog->ShowModal();
	wxYield();
        delete m_server;
        m_server = NULL;
	wxExit();
    }

    // Set the dialog icon
    //SetIcon(wxICON(vpex));

    // Create and position controls in the dialog

    // Status display
	statusDisplay = new wxStatusBar(this, Exec_StartStop, 0, _T("statusBar"));
	statusDisplay->SetStatusText(_T("VPEx Connection Status: DOWN"));
	SetStatusBar(statusDisplay);

    // Menu bar

#ifdef __WXMAC__
   wxMenuBar* menuBar = new wxMenuBar();
    wxMenu* testmenu = new  wxMenu();
	
    testmenu->Append(wxID_ABOUT, _("About"));
    testmenu->Append(wxID_EXIT, _("Exit"));
    testmenu->Append(Exec_ShowHideLogWindow, _("Show &Log Window\tAlt-L"));
    testmenu->Append(Exec_ShowHideManagementWindow, _("Show &Management Window\tAlt-M"));

    viewMenuSave = testmenu;
    
    menuBar->Append(testmenu, _("View"));
#else
    wxMenu *fileMenu = new wxMenu(wxEmptyString, wxMENU_TEAROFF);
    fileMenu->Append(Exec_Close, _T("E&xit\tAlt-X"), _T("Quit this program"));

    wxMenu *viewMenu = new wxMenu;
    viewMenu->Append(Exec_ShowHideLogWindow, _T("Show &Log Window\tAlt-L"),
                     _T("Show the log window"));
	viewMenu->Append(Exec_ShowHideManagementWindow, _T("Show &Management Window\tAlt-M"),
	                 _T("Show the management window"));
    viewMenuSave = viewMenu;

    wxMenu *helpMenu = new wxMenu;
    helpMenu->Append(Exec_About, _T("&About...\tAlt-A"),
                     _T("About this app"));

    // now append the freshly created menu to the menu bar...
    wxMenuBar *menuBar = new wxMenuBar();
    menuBar->Append(fileMenu, _T("&File"));
    menuBar->Append(viewMenu, _T("&View"));
    menuBar->Append(helpMenu, _T("&Help"));
#endif

    // ... and attach this menu bar to the frame
    SetMenuBar(menuBar);

    // Four command buttons.
    btnStartStop = new wxButton( this, Exec_StartStop,
	_T("Start VPEx Connection"), wxPoint(55,40), wxSize(200,60));
    btnLaunch = new wxButton( this, Exec_Launch,
	_T("Launch Admin Interface"), wxPoint(55,120), wxSize(200,60));
	
	wxStaticText *selectPrompt = new wxStaticText(this, -1, _T(""), wxPoint(5,15), wxDefaultSize, wxALIGN_LEFT);
	selectPrompt->SetLabel(_T("Select VPEx Connection:"));

	clientSelector = new wxComboBox( this, Exec_ClientSelected, _T("Choose VPEx Connection"), wxPoint(180,15), wxSize(100,30), 0, NULL, wxCB_READONLY | wxCB_SORT );

	// XXX in reality we'd traverse wxStandardPaths::GetUserDataDir and look for all *.conf files to populate this list
	enumerateVpexClients();
	
    // no default button

    // set up the log window
    logWindowFrame = new LogWindowFrame(_T("Log"),
                                wxDefaultPosition, wxSize(600, 300));
    logWindowFrame->mainWindow = this;

    managementFrame = new ManagementFrame(_T("BLAH"),
                                wxDefaultPosition, wxSize(600, 300));
    managementFrame->mainWindow = this;

    //logWindowFrame->Show(true); // the wxFrame doesn't know that it is visible : we have to tell it !
    //logWindowFrame->Show(false); // doing this, the frame will only be visible as a remanent image (only on slow computers)

    // Show it and tell the application that it's our main window
    logWindowFrame->SetBackgroundColour( wxColor( 255,255,255 ));
    managementFrame->SetBackgroundColour( wxColor( 255,255,255 ));

    // set up timer
    timer = new UpdateTimer(this);
    timer->start();
    timer->Notify();

    readyToUnarchiveVpexFiles = true;
}

// Originally a wxFrame doesn't have any method to set the
// window associated icon since this has been implemented in
// wxFrame only.
// But in a dialog based app we want to associate an icon
// to the main window (i.e. the dialog)
// This code is the same of wxFrame::SetIcon.
void MyFrame::SetIcon(const wxIcon& icon)
{
    m_icon = icon;
#ifdef __WIN95__
    if ( m_icon.Ok() )
        SendMessage((HWND) GetHWND(), WM_SETICON,
                    (WPARAM)TRUE, (LPARAM)(HICON) m_icon.GetHICON());
#endif
}

bool MyFrame::HandleProcessInput()
{
    bool hasInput = false;

	if (vpexProcessPid != 0)  {
        if (vpexProcess && vpexProcess->HasInput())
            hasInput = true;
    }

    return hasInput;
}

// idle handler
void MyFrame::OnIdle(wxIdleEvent& event)
{
    if (HandleProcessInput())
        event.RequestMore();
    event.Skip();
}

// misc utility funcs

void MyFrame::LaunchVpexConnection(const wxString &connectionName, const wxString &password)
{
	// FINDME
	
	// this is VERY hackish
	wxString tempPath = wxStandardPaths::Get().GetTempDir();
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	
	wxString tempFilePath = wxFileName::CreateTempFileName(tempPath);
	wxTextFile *tempFile = new wxTextFile(tempFilePath);
	tempFile->Create();
	tempFile->AddLine(password);
	tempFile->Write();
	tempFile->Close();

	::wxSetWorkingDirectory(configPath);
	
#ifdef __WINDOWS__
	//wxString cmdString = _T("START /min \"");
	wxString cmdString = _T("\"");
	cmdString << wxStandardPaths::Get().GetPluginsDir() << _("\\OPENVPN.EXE\" --config ");
	cmdString << connectionName << _T(".CONF --askpass ");
#elif defined(__WXMAC__) || defined(__unix)
	// app is in GetExecutablePath, but strip off the last part and replace it with openvpn
	// config file gets stashed in GetUserDataDir
	wxString cmdString = wxStandardPaths::Get().GetPluginsDir() << _("/openvpn --config ");
	cmdString << connectionName;
	cmdString << _(".conf --askpass ");
#endif
	
	
	
	cmdString << tempFilePath;
#ifdef __WINDOWS__
	//cmdString << "\"";
#endif

	wxLogDebug(cmdString);
/*
	wxMessageDialog *dialog = new wxMessageDialog(NULL, cmdString, wxT("HEY YOU!"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
	wxYield();
	int response = dialog->ShowModal();
	*/
	
	vpexProcess = new VPExProcess(logWindowFrame);
	vpexProcessPid = wxExecute(cmdString, wxEXEC_ASYNC , vpexProcess);
	
	if (vpexProcessPid == 0)  {
		wxString errorMsg = _T("Could not start VPEx connection.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
	}
}

void MyFrame::TerminateVpexConnection(void)
{
	if (vpexProcessPid != 0)  {
		if (wxKill(vpexProcessPid, wxSIGKILL, NULL, wxKILL_CHILDREN) != 0)  {
			wxString errorMsg = _T("Could not stop VPEx connection.");
			wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
			wxYield();
			int response = dialog->ShowModal();
		} else {
			vpexProcessPid = 0;
			delete vpexProcess;
		       vpexProcess = NULL;
		}
	} else {
		wxString errorMsg = _T("Could not stop VPEx connection.  (Perhaps no connection is actually running?)");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
	}
}

void MyFrame::HandleVpexFile(const wxString &fileName)
{
	/*
    const wxString name = wxString::Format(_T(".VPExConnectionManager.lock.%s"), wxGetUserId().c_str());
    wxSingleInstanceChecker *m_checker = new wxSingleInstanceChecker(name);
	*/

	wxYield();
	
	// create vpex config dir if it doesn't exist
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	if (!wxDir::Exists(configPath))  {
		// directory does not exist, let's make it
		if (!::wxMkdir(configPath))  {
			wxYield();
			wxString errorMsg = _T("Could not create VPEx configuration directory at ");
			errorMsg << configPath << _T(".");
			wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
			wxYield();
			int response = dialog->ShowModal();
		}
	}
	
	wxFileInputStream* file = new wxFileInputStream(fileName);
	wxZipInputStream* archiveFile = new wxZipInputStream(file);
	if ( archiveFile->IsOk() && archiveFile->CanRead() && (file->GetSize() != 0) ) {
		wxZipEntry* archiveFileMetaData;
	   	while ( (archiveFileMetaData = archiveFile->GetNextEntry()) != NULL ) {
			wxYield();
			wxString unzippedFileName = archiveFileMetaData->GetInternalName(); // gets name of the zipped file
	   		wxFileOutputStream unzippedFile(wxStandardPaths::Get().GetUserDataDir() + PATHSEP + unzippedFileName);
			archiveFile->Read(unzippedFile);
	   		unzippedFile.Close();
		}
		archiveFile->CloseEntry();
	}

	// attempt a connection
	wxYield();
	if (clientSelector)  {
		wxYield();
		wxString errorMsg = _T("New VPEx profile installed.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
		enumerateVpexClients();
	} else {
		wxYield();
		wxString errorMsg = _T("New VPEx profile installed.  It will be available the next time you start VPEx Connection Manager.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
		exit(0);
	}
}

void MyFrame::enumerateVpexClients(void)
{
	clientSelector->Clear();
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	if (!wxDir::Exists(configPath))  {
		// directory does not exist, let's make it
		if (!::wxMkdir(configPath))  {
			wxString errorMsg = _T("Could not create VPEx configuration directory at ");
			errorMsg << configPath << _T(".");
			wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
			wxYield();
			int response = dialog->ShowModal();
		}
	}
	wxDir *dir = new wxDir(configPath);
	int count = 0;
	
	//wxMessageDialog *dialog = new wxMessageDialog(NULL, configPath, wxT("Ahoy matey"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
	//int response = dialog->ShowModal();
	
    if ( !dir->IsOpened() )
    {
        // deal with the error here - wxDir would already log an error message
        // explaining the exact reason of the failure
		wxString errorMsg = _T("Could not find any VPEx client configuration files.  Ensure that at least one configuration is installed in ");
		errorMsg << configPath << _T(".");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
    }

    wxString filename;

    bool cont = dir->GetFirst(&filename, _T("*.conf"), wxDIR_FILES);
    while ( cont )
    {
		count++;
		clientSelector->Append(filename.BeforeLast(_T('.')));
        cont = dir->GetNext(&filename);
    }

	if (!count)  {
		wxString errorMsg = _T("Could not find any VPEx client configuration files.  Ensure that at least one configuration is installed in ");
		errorMsg << configPath << _T(".");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
	} else {
		clientSelector->SetSelection(0);
	}
}

void MyFrame::notifyOfNewConfiguration(void)
{
	wxString errorMsg = _T("New VPEx configuration added.");
	wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
	wxYield();
	int response = dialog->ShowModal();
}

// event handlers

void MyFrame::OnStartStop(wxCommandEvent& WXUNUSED(event))
{
    wxString password;

	    // see if openvpn is running
	#ifdef __WINDOWS__
	    if (FindProcByName("openvpn.exe"))  {
	#elif defined(__WXMAC__) || defined(__unix)
	    if (FindProcByName("openvpn"))  {
	#endif
	        // openvpn running.  kill it.
		TerminateVpexConnection();
		return;
	}

	// test for valid vpex connection
	wxString selectedConfig = clientSelector->GetValue();
	if (selectedConfig.IsEmpty())  {
		wxMessageDialog *dialog = new wxMessageDialog(NULL, wxT("Please choose a VPEx conection first."), wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		int response = dialog->ShowModal();
		return;
    } else {
        // openvpn NOT running.  Prompt for password and run it.
        wxPasswordEntryDialog* passwordFrame =
		new wxPasswordEntryDialog(NULL,
			wxT("Please enter your password to continue:"),
			wxT("Password entry dialog"));

        if (passwordFrame->ShowModal() == wxID_OK)
        {
            password = passwordFrame->GetValue();
            if (password == _T(""))  {
                wxMessageBox(_T("Blank passwords are not allowed."),
			_T("Error"), wxOK | wxICON_EXCLAMATION | wxCENTRE);

            } else {
				LaunchVpexConnection(selectedConfig, password);
            }
        }
    }
}

void MyFrame::OnLaunch(wxCommandEvent& WXUNUSED(event))
{
	char buf[128];
	sprintf(buf, "http://%s/", getTunAddress());
	wxString string = wxString::From8BitData(buf);
	::wxLaunchDefaultBrowser(string);
}

void MyFrame::OnQuitMenu(wxCommandEvent& event)
{
    if (ConfirmQuit())  {
	timer->stop();
    	if (isConnected)  {
		TerminateVpexConnection();
    	}
    	wxExit();
    }
    // else do nothing
}

void MyFrame::OnQuit(wxCloseEvent& event)
{
    // NOTE Since our main window is a dialog and not
    // ---- a frame we have to close it using Destroy
    //      rather than Close. In fact Close doesn't
    //      actually close a dialog but just hides it
    //      so that the application will hang there
    //      with his only window hidden and thus unable
    //      to get any user input.

    // --> Don't use Close with a wxFrame,
    //     use Destry instead.
    if (ConfirmQuit())  {
	timer->stop();
    	if (isConnected)  {
		TerminateVpexConnection();
    	}
    	wxExit();
    } else {
		event.Veto();
    }
}

bool MyFrame::ConfirmQuit(void)
{
	if (isConnected)  {
		// we only care if a connection is established
		wxMessageDialog *dialog = new wxMessageDialog(NULL, wxT("There is currently an active VPEx connection.  Are you sure you want to quit?"), wxT("Confirm Quit"), wxYES_NO | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		int response = dialog->ShowModal();
		if (response == wxID_YES)  {
			return true;
		} else {
			return false;
		}
	} else {
		return true;
	}
}

void MyFrame::OnAbout(wxCommandEvent& WXUNUSED(event))
{
    wxString msg;
    msg.Printf(_T("VPEx Connection Manager\n")
	       _T(VERSION_STRING)
               _T("\n(C) 2013 XOWare, Inc.  All rights reserved.\n")
	       _T("\nBuilt " __DATE__ " " __TIME__ "\n")
               _T("Written using %s")
#ifdef wxBETA_NUMBER
               _T(" (beta %d)")
#endif // wxBETA_NUMBER
               , wxVERSION_STRING
#ifdef wxBETA_NUMBER
               , wxBETA_NUMBER
#endif // wxBETA_NUMBER
              );

    wxMessageBox(msg, _T("About"), wxOK | wxICON_INFORMATION, this);
}

void MyFrame::SelectClient(wxCommandEvent& WXUNUSED(event))
{
	// nothing (yet...)
}

void MyFrame::ShowHideLogWindow(wxCommandEvent& WXUNUSED(event))
{
    if (!logWindowFrame->IsShownOnScreen())  {
    	logWindowFrame->Show(TRUE);
/*
	int id = viewMenuSave->FindItem(_T("Show &Log Window\tAlt-L"));
	viewMenuSave->SetLabel(id, _T("Hide &Log Window\tAlt-L"));
	viewMenuSave->SetHelpString(id, _T("Hide the log window"));
*/
    } else {
	logWindowFrame->Hide();
/*
	int id = viewMenuSave->FindItem(_T("Hide &Log Window\tAlt-L"));
	viewMenuSave->SetLabel(id, _T("Show &Log Window\tAlt-L"));
	viewMenuSave->SetHelpString(id, _T("Show the log window"));
*/
    }
}

void MyFrame::ShowHideManagementWindow(wxCommandEvent& WXUNUSED(event))
{
    if (!managementFrame->IsShownOnScreen())  {
    	managementFrame->Show(TRUE);
/*
	int id = viewMenuSave->FindItem(_T("Show &Log Window\tAlt-L"));
	viewMenuSave->SetLabel(id, _T("Hide &Log Window\tAlt-L"));
	viewMenuSave->SetHelpString(id, _T("Hide the log window"));
*/
    } else {
	managementFrame->Hide();
/*
	int id = viewMenuSave->FindItem(_T("Hide &Log Window\tAlt-L"));
	viewMenuSave->SetLabel(id, _T("Show &Log Window\tAlt-L"));
	viewMenuSave->SetHelpString(id, _T("Show the log window"));
*/
    }
}


// update timer

UpdateTimer::UpdateTimer(MyFrame* frame) : wxTimer()
{
    UpdateTimer::frame = frame;
    // load connection sounds
    wxString soundPath = wxStandardPaths::Get().GetResourcesDir();
    connectSound = new wxSound(soundPath << PATHSEP << _T("disconnect.wav"), false);
    disconnectSound = connectSound;
}

void UpdateTimer::Notify()
{
	static char buf[128];
#ifdef __WINDOWS__
    if (FindProcByName("openvpn.exe"))  {
#elif defined(__WXMAC__) || defined(__unix)
    if (FindProcByName("openvpn"))  {
#endif
        frame->statusDisplay->SetStatusText(_T("VPEx Connection Status: UP"));
        frame->btnStartStop->SetLabel(_T("Stop VPEx Connection"));
	frame->clientSelector->Enable(false);
	if (strlen(getTunAddress()) != 0)  {
		sprintf(buf, "VPEx Connection Status: UP, remote addr = %s", getTunAddress());
		wxString string = wxString::From8BitData(buf);
        	frame->statusDisplay->SetStatusText(string);
		frame->btnLaunch->Enable(true);
	} else {
		frame->btnLaunch->Enable(false);
	}
	if (!frame->isConnected)  {
		connectSound->Play(wxSOUND_ASYNC);
		frame->isConnected = true;
	}
    } else {
        frame->statusDisplay->SetStatusText(_T("VPEx Connection Status: DOWN"));
        frame->btnStartStop->SetLabel(_T("Start VPEx Connection"));
	frame->clientSelector->Enable(true);
	frame->btnLaunch->Enable(false);
	if (frame->isConnected)  {
		disconnectSound->Play(wxSOUND_ASYNC);
		frame->isConnected = false;
	}
    }
    frame->Refresh();

    // now update menu
    if (frame->viewMenuSave)  {
    	if (frame->logWindowFrame->IsShownOnScreen())  {
        	int id = frame->viewMenuSave->FindItem(_T("Show &Log Window\tAlt-L"));
			if(id != wxNOT_FOUND)  {
        		frame->viewMenuSave->SetLabel(id, _T("Hide &Log Window\tAlt-L"));
        		frame->viewMenuSave->SetHelpString(id, _T("Hide the log window"));
			}
    	} else {
        	int id = frame->viewMenuSave->FindItem(_T("Hide &Log Window\tAlt-L"));
			if(id != wxNOT_FOUND)  {
        		frame->viewMenuSave->SetLabel(id, _T("Show &Log Window\tAlt-L"));
        		frame->viewMenuSave->SetHelpString(id, _T("Show the log window"));
			}
    	}
    	if (frame->managementFrame->IsShownOnScreen())  {
        	int id = frame->viewMenuSave->FindItem(_T("Show &Management Window\tAlt-M"));
			if(id != wxNOT_FOUND)  {
        		frame->viewMenuSave->SetLabel(id, _T("Hide &Management Window\tAlt-M"));
        		frame->viewMenuSave->SetHelpString(id, _T("Hide the management window"));
			}
    	} else {
        	int id = frame->viewMenuSave->FindItem(_T("Hide &Management Window\tAlt-M"));
			if(id != wxNOT_FOUND)  {
        		frame->viewMenuSave->SetLabel(id, _T("Show &Management Window\tAlt-M"));
        		frame->viewMenuSave->SetHelpString(id, _T("Show the management window"));
			}
    	}
    }
}

void UpdateTimer::start()
{
    wxTimer::Start(1000);
}

void UpdateTimer::stop()
{
    wxTimer::Stop();
}


UnarchiveThread::UnarchiveThread(MyFrame *f, wxMessageQueue<wxString> *q)
{
	frame = f;
	queue = q;
}

void *UnarchiveThread::Entry()
{
	wxString filename;
	// this is not an error: this thread runs continually
	while (true)  {
		queue->Receive(filename);
		// XXX DO SOMETHING
		//
		// create vpex config dir if it doesn't exist
		wxString configPath = wxStandardPaths::Get().GetUserDataDir();
		/*
		if (!wxDir::Exists(configPath))  {
			// directory does not exist, let's make it
			if (!::wxMkdir(configPath))  {
				// an error occurred
			}
		}
		*/
	
		wxFileInputStream* file = new wxFileInputStream(filename);
		wxZipInputStream* archiveFile = new wxZipInputStream(file);
		if ( archiveFile->IsOk() && archiveFile->CanRead() && (file->GetSize() != 0) ) {
			wxZipEntry* archiveFileMetaData;
	   		while ( (archiveFileMetaData = archiveFile->GetNextEntry()) != NULL ) {
				wxString unzippedFileName = archiveFileMetaData->GetInternalName(); // gets name of the zipped file
	   			wxFileOutputStream unzippedFile(wxStandardPaths::Get().GetUserDataDir() + PATHSEP + unzippedFileName);
				archiveFile->Read(unzippedFile);
	   			unzippedFile.Close();
			}
			archiveFile->CloseEntry();
		}
		if (frame)  {
			frame->enumerateVpexClients();
			frame->notifyOfNewConfiguration();
		}
	}
}

void UnarchiveThread::OnExit()
{
}
