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
#include "FindProcByName.h"

// ----------------------------------------------------------------------------
// private classes
// ----------------------------------------------------------------------------

// Define a new dialog type: this is going to be our main window
#ifndef _LOGWINDOWFRAME_H
#define _LOGWINDOWFRAME_H
class LogWindowFrame : public wxFrame
{
public:
    // Constructor(s)
    LogWindowFrame(const wxString& title, const wxPoint& pos, const wxSize& size);

    // Event handlers (these functions should _not_ be virtual)
    void OnClose(wxCloseEvent& event);

    // Set icon (from wxFrame source code)
    virtual void SetIcon(const wxIcon& icon);


    // accept input
    void ReadVPExOutput(const wxString& line, bool isErrorStream);

    // Add to list box
    void AddLogItem(const char *logItem);

    // pointer back to main window (this is ugly but whatever)
    void *mainWindow;

private:
    // List Box
    wxListBox *log;

    // update timer
    //UpdateTimer *timer;

    // Frame icon
    wxIcon m_icon;

    // Any class wishing to process wxWindows events must use this macro
    DECLARE_EVENT_TABLE()
};
#endif
