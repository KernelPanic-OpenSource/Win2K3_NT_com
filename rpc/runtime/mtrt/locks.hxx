/*++

Copyright (C) Microsoft Corporation, 1995 - 1999

Module Name:

    Locks.hxx

Abstract:

    Several small class (CInterlockedInteger, CMutexLock and CSharedLock
    which are wrappers for Win32 APIs.

    CInterlockedInteger is a simple wrapper for win32 interlockedm integer APis.

    CMutexLock is a wrapper designed to automatically constructed around
    win32 critical sections.  They never forget to release the lock.

    CSharedLocks are similar to NT resources, they allow shared (multiple readers)
    and exclusive (single writer) access to the resource.  They are different
    in the following ways:
        CSharedLocks don't starve exclusive threads.
        Exclusive threads spin (Sleep(0)) while waiting for readers to finish.
        Exclusive threads can gain shared access iff the shared access is properly
    contained in the exclusive lock.  While performing recursive locking the thread
    must hold an exlusive lock.
        Exclusive threads can recursively take the lock.
        Exclusive threads always block if they can't get access.

Author:

    Mario Goertzel    [MarioGo]

Revision History:

    MarioGo     03-14-95    Moved from misc.?xx

--*/

#ifndef __LOCKS_HXX
#define __LOCKS_HXX

class CInterlockedInteger
    {
    private:
    LONG i;

    public:

    CInterlockedInteger(LONG i = 0) : i(i) {}

    LONG operator++(int)
        {
        return(InterlockedIncrement(&i));
        }

    LONG operator--(int)
        {
        return(InterlockedDecrement(&i));
        }

    operator LONG()
        {
        return(i);
        }
    };

class CMutexLock
    {
    private:

    CRITICAL_SECTION *pCurrentLock;
    int owned;

    public:

    CMutexLock(CRITICAL_SECTION *pLock) : owned(0), pCurrentLock(pLock) {
        Lock();
        }

    ~CMutexLock() {
        if (owned)
            Unlock();
#if DBG
        pCurrentLock = 0;
#endif
        }

    void Lock()
        {
        ASSERT(!owned);
        EnterCriticalSection(pCurrentLock);
        owned = 1;
        }

    void Unlock()
        {
        ASSERT(owned);
        LeaveCriticalSection(pCurrentLock);
        owned = 0;
        }
    };


class CSharedLock
    {
    private:
    CRITICAL_SECTION    lock;
    HANDLE              hevent;
    CInterlockedInteger readers;
    LONG                writers;
    DWORD               exclusive_owner;
    unsigned            recursion_count;

    public:

    CSharedLock(RPC_STATUS &status);

    ~CSharedLock();

    void LockShared(void);

    void UnlockShared(void);

    void LockExclusive(void);

    void UnlockExclusive(void);

    void Unlock(void);

    void ConvertToExclusive(void);

    BOOL HeldExclusive()
        {
        return(exclusive_owner == GetCurrentThreadId());
        }

    BOOL NotHeldExclusiveByAnyone()
        {
        return(exclusive_owner == 0);
        }
    };

#endif // __LOCKS_HXX

