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

#include <wx/process.h>
#include <wx/timer.h>

#include <wx/stdpaths.h>
#include <wx/dir.h>

#include "IPCClient.h"
#include "IPCClientConnection.h"

#include "ManagementFrame.h"

// ----------------------------------------------------------------------------
// private classes
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// constants
// ----------------------------------------------------------------------------

// IDs for the controls in the dialog
enum
{
    Exec_Close = 1,
	Exec_ClientSelected,
	Exec_DeleteButtonClicked
};

// ----------------------------------------------------------------------------
// event tables and other macros for wxWindows
// ----------------------------------------------------------------------------

BEGIN_EVENT_TABLE(ManagementFrame, wxFrame)
    EVT_CLOSE(ManagementFrame::OnClose)
	EVT_LISTBOX(Exec_ClientSelected, ManagementFrame::SelectClient)
	EVT_BUTTON(Exec_DeleteButtonClicked, ManagementFrame::DeleteClient)
END_EVENT_TABLE()

// ============================================================================
// implementation
// ============================================================================

// ----------------------------------------------------------------------------
// main dialog
// ----------------------------------------------------------------------------

// Frame constructor
ManagementFrame::ManagementFrame(const wxString& title, const wxPoint& pos, const wxSize& size)
        : wxFrame((wxFrame *)NULL, -1, title, pos, size)
{
    // Set the dialog icon
    //SetIcon(wxICON(vpex));

    // Create and position controls in the dialog

    // create the listbox in which we will show misc messages as they come
	clientSelector = new wxListBox( this, Exec_ClientSelected, wxPoint(0,0), wxSize(300,300), 0, NULL, wxLB_SINGLE | wxLB_SORT | wxLB_ALWAYS_SB );

    btnDelete = new wxButton( this, Exec_DeleteButtonClicked,
	_T("Delete"), wxPoint(400,130), wxSize(150,40));

    // set up timer
    //timer = new UpdateTimer(this);
    //timer->start();
    //timer->Notify();

	enumerateVpexClients();
}

// Originally a wxFrame doesn't have any method to set the
// window associated icon since this has been implemented in
// wxFrame only.
// But in a dialog based app we want to associate an icon
// to the main window (i.e. the dialog)
// This code is the same of wxFrame::SetIcon.
void ManagementFrame::SetIcon(const wxIcon& icon)
{
    m_icon = icon;
#ifdef __WIN95__
    if ( m_icon.Ok() )
        SendMessage((HWND) GetHWND(), WM_SETICON,
                    (WPARAM)TRUE, (LPARAM)(HICON) m_icon.GetHICON());
#endif
}

void ManagementFrame::OnClose(wxCloseEvent& WXUNUSED(event))
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
    //timer->stop();
    //wxMessageBox("Moriturum te saluto.", "Adios amigo...", wxOK | wxCENTRE);
    //wxExit();
    //XXX send mesage to main window telling them that log window was closed

    Hide();

    //Destroy();
}

void ManagementFrame::SelectClient(wxCommandEvent& WXUNUSED(event))
{
	// nothing (yet...)
}

void ManagementFrame::DeleteClient(wxCommandEvent& WXUNUSED(event))
{
	int sel = clientSelector->GetSelection();
	if (sel != wxNOT_FOUND)  {
		wxString selectedConfig = clientSelector->GetString(sel);
		wxString confirmString = _T("Do you really want to delete VPEx configuration `");
		confirmString << selectedConfig << _T("'?  This action cannot be undone.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, confirmString, wxT("Confirm Delete"), wxYES_NO | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		int response = dialog->ShowModal();
		if (response == wxID_YES)  {
			removeAllFilesStartingWith(selectedConfig);
			enumerateVpexClients();
			notifyOfConfigurationChange();
		}
	} else {
		wxString errorMsg = _T("Please select a VPEx connection first.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Error"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		int response = dialog->ShowModal();
	}
}

void ManagementFrame::enumerateVpexClients(void)
{
	clientSelector->Clear();
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	wxDir *dir = new wxDir(configPath);
	int count = 0;
	
	//wxMessageDialog *dialog = new wxMessageDialog(NULL, configPath, wxT("Ahoy matey"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
	//int response = dialog->ShowModal();
	
    if ( !dir->IsOpened() )
    {
        // deal with the error here - wxDir would already log an error message
        // explaining the exact reason of the failure
		wxString errorMsg = _T("Could not access VPEx client configuration directory.");
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

	if (count)  {
		clientSelector->SetSelection(0);
	}
}

void ManagementFrame::notifyOfConfigurationChange(void)
{
	m_client = new IPCClient;
    bool retval = m_client->Connect(_("localhost"), _("4242"), _("VPEx"));

    if (retval)  {
        wxString s = wxDateTime::Now().Format();
        m_client->GetConnection()->Poke(_T("Date"), (wxChar *)s.c_str());
	} else {
		wxString errorMsg = _T("Could not contact client update notification service.  The list of VPEx clients will not be refreshed until you quit and relaunch VPEx Connection Manager.");
		wxMessageDialog *dialog = new wxMessageDialog(NULL, errorMsg, wxT("Notice"), wxOK | wxICON_EXCLAMATION | wxSTAY_ON_TOP);
		wxYield();
		int response = dialog->ShowModal();
		// XXX brutal hack
    }
    delete m_client;
    m_client = NULL;
}

void ManagementFrame::removeAllFilesStartingWith(wxString string)
{
	wxString configPath = wxStandardPaths::Get().GetUserDataDir();
	wxString fileSpec = string;
	fileSpec << _T("*.*");
	wxArrayString files;
	size_t numberOfFiles = wxDir::GetAllFiles(configPath, &files, fileSpec, wxDIR_FILES);
	for (int i = 0; i < numberOfFiles; i++)  {
		wxRemoveFile(files[i]);
	}
}

// update timer

/* UpdateTimer::UpdateTimer(ManagementFrame* frame) : wxTimer()
{
    UpdateTimer::frame = frame;
}

void UpdateTimer::Notify()
{
    //wxMessageBox("TIMER!", "Yoo-hoo!", wxOK | wxICON_INFORMATION);

    if (FindProcByName("openvpn.exe"))  {
        frame->statusDisplay->SetLabel("VPEx Connection Status: UP");
        frame->btnStartStop->SetLabel("Stop VPEx Connection");
    } else {
        frame->statusDisplay->SetLabel("VPEx Connection Status: DOWN");
        frame->btnStartStop->SetLabel("Start VPEx Connection");
    }
    frame->Refresh();
}

void UpdateTimer::start()
{
    wxTimer::Start(1000);
}

void UpdateTimer::stop()
{
    wxTimer::Stop();
}
*/
