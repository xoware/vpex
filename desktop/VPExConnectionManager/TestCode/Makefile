all:	iptest.exe iptest

iptest.exe:	iptest.cpp NetUtils.cpp NetUtils.h
	i586-mingw32msvc-g++ -o iptest.exe iptest.cpp NetUtils.cpp -liphlpapi              

iptest:	iptest.cpp NetUtils.cpp NetUtils.h
	g++     iptest.cpp NetUtils.cpp  -o iptest

clean:
	rm -f *.o iptest.exe iptest
