#include <wx/wx.h>
#include <wx/txtstrm.h>

#include "VPExProcess.h"

IMPLEMENT_CLASS(VPExProcess, PipedProcess)

void VPExProcess::OnTerminate(int pid, int status)
{
    // show the rest of the output
    while ( HasInput() )
        ;

    //m_logWindow->VPExTerminated();

    //delete this;
}

bool VPExProcess::HasInput()
{
    bool hasInput = false;
    static wxChar buffer[4096];

    if ( !m_input.IsEmpty() )
    {
        wxTextOutputStream os(*GetOutputStream());
        os.WriteString(m_input);
        m_input.Empty();

        hasInput = true;
    }

    if ( IsErrorAvailable() )
    {
        buffer[GetErrorStream()->Read(buffer, WXSIZEOF(buffer) - 1).LastRead()] = _T('\0');
        wxString msg(buffer);

        m_logWindow->ReadVPExOutput(msg, true);

        hasInput = true;
    }

    if ( IsInputAvailable() )
    {
        buffer[GetInputStream()->Read(buffer, WXSIZEOF(buffer) - 1).LastRead()] = _T('\0');
        wxString msg(buffer);

        m_logWindow->ReadVPExOutput(buffer, false);

        hasInput = true;
    }

    return hasInput;
}

/// Feed it some input
void VPExProcess::SendInput(const wxString& text)
{
    m_input = text;
}
