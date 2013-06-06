/*
 * Base class for a piped process
 */

#include <wx/wx.h>
#include <wx/process.h>

class PipedProcess : public wxProcess
{
    DECLARE_CLASS(PipedProcess)
public:
    PipedProcess(wxWindow* win): wxProcess(win, wxPROCESS_REDIRECT) {}

    virtual bool HasInput() = 0;
};
