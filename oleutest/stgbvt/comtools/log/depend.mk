# 
# Built automatically 
# 
 
# 
# Source files 
# 
 
$(OBJDIR)\log.obj $(OBJDIR)\log.lst: .\log.cxx $(CRTINC)\assert.h \
	$(CRTINC)\ctype.h $(CRTINC)\excpt.h $(CRTINC)\stdarg.h \
	$(CRTINC)\stddef.h $(CRTINC)\stdio.h $(CRTINC)\stdlib.h \
	$(CRTINC)\string.h $(OSINC)\cderr.h $(OSINC)\commdlg.h $(OSINC)\dde.h \
	$(OSINC)\ddeml.h $(OSINC)\dlgs.h $(OSINC)\drivinit.h \
	$(OSINC)\lzexpand.h $(OSINC)\mmsystem.h $(OSINC)\nb30.h \
	$(OSINC)\ole.h $(OSINC)\rpc.h $(OSINC)\rpcdce.h $(OSINC)\rpcdcep.h \
	$(OSINC)\rpcnsi.h $(OSINC)\rpcnterr.h $(OSINC)\shellapi.h \
	$(OSINC)\winbase.h $(OSINC)\wincon.h $(OSINC)\windef.h \
	$(OSINC)\windows.h $(OSINC)\winerror.h $(OSINC)\wingdi.h \
	$(OSINC)\winnetwk.h $(OSINC)\winnls.h $(OSINC)\winnt.h \
	$(OSINC)\winperf.h $(OSINC)\winreg.h $(OSINC)\winsock.h \
	$(OSINC)\winspool.h $(OSINC)\winsvc.h $(OSINC)\winuser.h \
	$(OSINC)\winver.h ..\h\log.hxx ..\h\syncwrap.hxx .\log.h

$(OBJDIR)\common.obj $(OBJDIR)\common.lst: .\common.cxx $(CRTINC)\direct.h \
	$(CRTINC)\fcntl.h $(CRTINC)\io.h $(CRTINC)\sys\stat.h \
	$(CRTINC)\sys\types.h $(CRTINC)\time.h $(CRTINC)\assert.h \
	$(CRTINC)\ctype.h $(CRTINC)\excpt.h $(CRTINC)\stdarg.h \
	$(CRTINC)\stddef.h $(CRTINC)\stdio.h $(CRTINC)\stdlib.h \
	$(CRTINC)\string.h $(OSINC)\lmapibuf.h $(OSINC)\lmcons.h \
	$(OSINC)\lmerr.h $(OSINC)\lmuseflg.h $(OSINC)\lmwksta.h \
	$(OSINC)\cderr.h $(OSINC)\commdlg.h $(OSINC)\dde.h $(OSINC)\ddeml.h \
	$(OSINC)\dlgs.h $(OSINC)\drivinit.h $(OSINC)\lzexpand.h \
	$(OSINC)\mmsystem.h $(OSINC)\nb30.h $(OSINC)\ole.h $(OSINC)\rpc.h \
	$(OSINC)\rpcdce.h $(OSINC)\rpcdcep.h $(OSINC)\rpcnsi.h \
	$(OSINC)\rpcnterr.h $(OSINC)\shellapi.h $(OSINC)\winbase.h \
	$(OSINC)\wincon.h $(OSINC)\windef.h $(OSINC)\windows.h \
	$(OSINC)\winerror.h $(OSINC)\wingdi.h $(OSINC)\winnetwk.h \
	$(OSINC)\winnls.h $(OSINC)\winnt.h $(OSINC)\winperf.h \
	$(OSINC)\winreg.h $(OSINC)\winsock.h $(OSINC)\winspool.h \
	$(OSINC)\winsvc.h $(OSINC)\winuser.h $(OSINC)\winver.h ..\h\log.hxx \
	..\h\syncwrap.hxx .\log.h

