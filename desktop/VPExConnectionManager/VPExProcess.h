#include <wx/wx.h>
#include <wx/process.h>

#include "PipedProcess.h"
#include "LogWindowFrame.h"

class VPExProcess : public PipedProcess
{
public:
    DECLARE_CLASS(VPExProcess)

    VPExProcess(LogWindowFrame* window): PipedProcess(window), m_logWindow(window)
    {
        Redirect();
    }

    virtual void OnTerminate(int pid, int status);

    virtual bool HasInput();

    /// Feed it some input
    void SendInput(const wxString& text);

protected:
    LogWindowFrame*   m_logWindow;
    wxString          m_input; // to send to process
};
