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

#include "LogWindowFrame.h"

// ----------------------------------------------------------------------------
// private classes
// ----------------------------------------------------------------------------

// ----------------------------------------------------------------------------
// constants
// ----------------------------------------------------------------------------

// IDs for the controls in the dialog
enum
{
    Exec_Close = 1
};

// ----------------------------------------------------------------------------
// event tables and other macros for wxWindows
// ----------------------------------------------------------------------------

BEGIN_EVENT_TABLE(LogWindowFrame, wxFrame)
    EVT_CLOSE(LogWindowFrame::OnClose)
END_EVENT_TABLE()

// ============================================================================
// implementation
// ============================================================================

// ----------------------------------------------------------------------------
// main dialog
// ----------------------------------------------------------------------------

// Frame constructor
LogWindowFrame::LogWindowFrame(const wxString& title, const wxPoint& pos, const wxSize& size)
        : wxFrame((wxFrame *)NULL, -1, title, pos, size)
{
    // Set the dialog icon
    //SetIcon(wxICON(vpex));

    // Create and position controls in the dialog

    // create the listbox in which we will show misc messages as they come
	log = new wxListBox(this, wxID_ANY, wxDefaultPosition, wxDefaultSize, 0, NULL, wxLB_HSCROLL|wxLB_ALWAYS_SB, wxDefaultValidator, _T("listBox"));
	//log = new wxListBox(this, wxID_ANY, wxLB_HSCROLL|wxLB_ALWAYS_SB);

    wxFont font(10, wxFONTFAMILY_TELETYPE, wxFONTSTYLE_NORMAL,
                wxFONTWEIGHT_NORMAL);
    if ( font.Ok() )
        log->SetFont(font);

                wxDateTime now=wxDateTime::Now();

                wxString logMsg = _("--- VPEx Connection Manager started at ");
		logMsg << now.Format();
		char cstring[1024];
		strncpy(cstring, (const char*)logMsg.mb_str(wxConvUTF8), 1023);
                //wxCharBuffer buf = logMsg.ToUTF8();
                this->AddLogItem(cstring);

	Maximize();
    
	// set up timer
    //timer = new UpdateTimer(this);
    //timer->start();
    //timer->Notify();
}

// Originally a wxFrame doesn't have any method to set the
// window associated icon since this has been implemented in
// wxFrame only.
// But in a dialog based app we want to associate an icon
// to the main window (i.e. the dialog)
// This code is the same of wxFrame::SetIcon.
void LogWindowFrame::SetIcon(const wxIcon& icon)
{
    m_icon = icon;
#ifdef __WIN95__
    if ( m_icon.Ok() )
        SendMessage((HWND) GetHWND(), WM_SETICON,
                    (WPARAM)TRUE, (LPARAM)(HICON) m_icon.GetHICON());
#endif
}

void LogWindowFrame::ReadVPExOutput(const wxString& line, bool isErrorStream)
{
	char cstring[1024];
	char buf[1024];
	char *p = cstring, *q = buf;
	strncpy(cstring, (const char*)line.mb_str(wxConvUTF8), 1023);
	p = cstring;
	while (p != NULL && *p != NULL)  {
		if (*p == '\n')  {
			*q = '\0';
			this->AddLogItem(buf);
			q = buf;
		} else if (*p != '\r') {
			*q = *p;
			++q;
		}
		++p;
	}
	//this->AddLogItem(cstring);
}

void LogWindowFrame::OnClose(wxCloseEvent& WXUNUSED(event))
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

void LogWindowFrame::AddLogItem(const char *logItem)
{
	log->Append(wxString::From8BitData(logItem));
	log->SetSelection(log->GetCount()-1);
	//log->Thaw();
}

// update timer

/* UpdateTimer::UpdateTimer(LogWindowFrame* frame) : wxTimer()
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
