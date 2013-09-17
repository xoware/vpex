DEBUG?="NO"

CC=$(BUILDDIR)/mingw32/bin/i586-mingw32msvc-g++
WINDRES=$(BUILDDIR)/mingw32/bin/i586-mingw32msvc-windres
WX_CFLAGS=$(shell $(BUILDDIR)/mingw32/bin/wx-config --debug=$(DEBUG) --cxxflags | sed -e 's/-mthreads//g')
CFLAGS=-g $(DEBUG_FLAG) $(WX_CFLAGS)  -Wno-deprecated -Wall

WX_LDFLAGS=$(shell $(BUILDDIR)/mingw32/bin/wx-config --debug=$(DEBUG) --libs | sed -e 's/-mthreads//g')
LDFLAGS=-g $(DEBUG_FLAG) -static-libgcc -static-libstdc++  -L$(BUILDDIR)/mingw32/lib $(WX_LDFLAGS) -liphlpapi
LIBGCC_DIR=$(shell dirname `i586-mingw32msvc-g++ -print-libgcc-file-name`)


all:			VPExConnectionManager.exe

VPExConnectionManager.exe:		VPExConnectionManager.o VPExConnectionManager.res FindProcByName.o \
				LogWindowFrame.o ManagementFrame.o VPExProcess.o PipedProcess.o NetUtils.o  \
				IPCClient.o IPCClientConnection.o IPCServer.o IPCServerConnection.o
			$(CC) -o VPExConnectionManager.exe VPExConnectionManager.o \
				VPExConnectionManager.res FindProcByName.o LogWindowFrame.o ManagementFrame.o \
				VPExProcess.o PipedProcess.o NetUtils.o \
				IPCClient.o IPCClientConnection.o IPCServer.o IPCServerConnection.o \
				$(LDFLAGS)

VPExConnectionManager.o:		VPExConnectionManager.cpp VPExConnectionManager.h VPExProcess.h FindProcByName.h LogWindowFrame.h
			$(CC) $(CFLAGS) -c VPExConnectionManager.cpp

VPExConnectionManager.res:		VPExConnectionManager.rc
			$(WINDRES) -I$(BUILDDIR)/mingw32/include/wx-2.8 VPExConnectionManager.rc -O coff -o VPExConnectionManager.res

FindProcByName.o:	FindProcByName.c FindProcByName.h
			$(CC) $(CFLAGS) -c FindProcByName.c

LogWindowFrame.o:	LogWindowFrame.cpp
			$(CC) $(CFLAGS) -c LogWindowFrame.cpp

ManagementFrame.o:	ManagementFrame.cpp
			$(CC) $(CFLAGS) -c ManagementFrame.cpp

VPExProcess.o:	VPExProcess.cpp VPExProcess.h
			$(CC) $(CFLAGS) -c VPExProcess.cpp

PipedProcess.o:	PipedProcess.cpp PipedProcess.h
			$(CC) $(CFLAGS) -c PipedProcess.cpp

NetUtils.o:		NetUtils.cpp NetUtils.h
			$(CC) $(CFLAGS) -c NetUtils.cpp

#cJSON.o:		cJSON.c cJSON.h
#			$(CC) $(CFLAGS) -c cJSON.c

IPCClient.o:		IPCClient.cpp IPCClient.h IPCClientConnection.h ipcsetup.h
			$(CC) $(CFLAGS) -c IPCClient.cpp

IPCClientConnection.o:	IPCClientConnection.cpp IPCClientConnection.h ipcsetup.h
			$(CC) $(CFLAGS) -c IPCClientConnection.cpp

IPCServer.o:		IPCServer.cpp IPCServer.h IPCServerConnection.h ipcsetup.h
			$(CC) $(CFLAGS) -c IPCServer.cpp

IPCServerConnection.o:	IPCServerConnection.cpp IPCServerConnection.h ipcsetup.h
			$(CC) $(CFLAGS) -c IPCServerConnection.cpp

install:		VPExConnectionManager.exe
			cp VPExConnectionManager.exe $(PREFIX)/VPExConnectionManager.exe
			cp stopvpex.bat $(PREFIX)/stopvpex.bat
			cp splash.png $(PREFIX)/splash.png
			cp connect.wav $(PREFIX)/connect.wav
			cp disconnect.wav $(PREFIX)/disconnect.wav
			cp $(BUILDDIR)/mingw32/lib/mingwm10.dll $(PREFIX)/mingwm10.dll
			cp -v $(LIBGCC_DIR)/*.dll $(PREFIX)
			for FILE in wxbase28*_gcc_custom.dll wxbase28*_net_gcc_custom.dll wxbase28*_xml_gcc_custom.dll wxmsw28*_adv_gcc_custom.dll wxmsw28*_aui_gcc_custom.dll wxmsw28*_core_gcc_custom.dll wxmsw28*_html_gcc_custom.dll wxmsw28*_qa_gcc_custom.dll wxmsw28*_richtext_gcc_custom.dll wxmsw28*_xrc_gcc_custom.dll ; do cp $(BUILDDIR)/mingw32/lib/$$FILE $(PREFIX) ; done

clean:
			rm -f *.exe *.o *.res
