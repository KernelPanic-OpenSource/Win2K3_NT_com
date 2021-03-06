// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*
 * Generational GC handle manager.  Core Table Implementation.
 *
 * Implementation of core table management routines.
 *
 * francish
 */

#include "common.h"
#include "HandleTablePriv.h"



/****************************************************************************
 *
 * RANDOM HELPERS
 *
 ****************************************************************************/

//@TODO: put this lookup in a read only data or code section
BYTE c_rgLowBitIndex[256] =
{
    0xff, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x06, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x07, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x06, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x05, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
    0x03, 0x00, 0x01, 0x00, 0x02, 0x00, 0x01, 0x00,
};


/*
 * A 32/64 neutral quicksort
 */
//@TODO: move/merge into common util file
typedef int (*PFNCOMPARE)(UINT_PTR p, UINT_PTR q);
void QuickSort(UINT_PTR *pData, int left, int right, PFNCOMPARE pfnCompare)
{
    do
    {
        int i = left;
        int j = right;

        UINT_PTR x = pData[(i + j + 1) / 2];

        do
        {
            while (pfnCompare(pData[i], x) < 0)
                i++;

            while (pfnCompare(x, pData[j]) < 0)
                j--;

            if (i > j)
                break;

            if (i < j)
            {
                UINT_PTR t = pData[i];
                pData[i] = pData[j];
                pData[j] = t;
            }

            i++;
            j--;

        } while (i <= j);

        if ((j - left) <= (right - i))
        {
            if (left < j)
                QuickSort(pData, left, j, pfnCompare);

            left = i;
        }
        else
        {
            if (i < right)
                QuickSort(pData, i, right, pfnCompare);

            right = j;
        }

    } while (left < right);
}


/*
 * CompareHandlesByFreeOrder
 *
 * Returns:
 *  <0 - handle P should be freed before handle Q
 *  =0 - handles are eqivalent for free order purposes
 *  >0 - handle Q should be freed before handle P
 *
 */
int CompareHandlesByFreeOrder(UINT_PTR p, UINT_PTR q)
{
    // compute the segments for the handles
    TableSegment *pSegmentP = (TableSegment *)(p & HANDLE_SEGMENT_ALIGN_MASK);
    TableSegment *pSegmentQ = (TableSegment *)(q & HANDLE_SEGMENT_ALIGN_MASK);

    // are the handles in the same segment?
    if (pSegmentP == pSegmentQ)
    {
        // return the in-segment handle free order
        return (int)((INT_PTR)q - (INT_PTR)p);
    }
    else if (pSegmentP)
    {
        // do we have two valid segments?
        if (pSegmentQ)
        {
            // return the sequence order of the two segments
            return (int)(UINT)pSegmentQ->bSequence - (int)(UINT)pSegmentP->bSequence;
        }
        else
        {
            // only the P handle is valid - free Q first
            return 1;
        }
    }
    else if (pSegmentQ)
    {
        // only the Q handle is valid - free P first
        return -1;
    }

    // neither handle is valid
    return 0;
}


/*
 * ZeroHandles
 *
 * Zeroes the object pointers for an array of handles.
 *
 */
void ZeroHandles(OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // compute our stopping point
    OBJECTHANDLE *pLastHandle = pHandleBase + uCount;

    // loop over the array, zeroing as we go
    while (pHandleBase < pLastHandle)
    {
        // get the current handle from the array
        OBJECTHANDLE handle = *pHandleBase;

        // advance to the next handle
        pHandleBase++;

        // zero the handle's object pointer
        *(_UNCHECKED_OBJECTREF *)handle = NULL;
    }
}

#ifdef _DEBUG
void CALLBACK DbgCountEnumeratedBlocks(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo)
{
    // accumulate the block count in pInfo->param1
    pInfo->param1 += uCount;
}
#endif

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE MANAGEMENT
 *
 ****************************************************************************/

/*
 * TableCanFreeSegmentNow
 *
 * Determines if it is OK to free the specified segment at this time.
 *
 */
BOOL TableCanFreeSegmentNow(HandleTable *pTable, TableSegment *pSegment)
{
    // sanity
    _ASSERTE(pTable);
    _ASSERTE(pSegment);
    _ASSERTE(pTable->pLock->OwnedByCurrentThread());

    // deterine if any segment is currently being scanned asynchronously
    TableSegment *pSegmentAsync = NULL;

    // do we have async info?
    AsyncScanInfo *pAsyncInfo = pTable->pAsyncScanInfo;
    if (pAsyncInfo)
    {
        // must always have underlying callback info in an async scan
        _ASSERTE(pAsyncInfo->pCallbackInfo);

        // yes - if a segment is being scanned asynchronously it is listed here
        pSegmentAsync = pAsyncInfo->pCallbackInfo->pCurrentSegment;
    }

    // we can free our segment if it isn't being scanned asynchronously right now
    return (pSegment != pSegmentAsync);
}


/*
 * BlockFetchUserDataPointer
 *
 * Gets the user data pointer for the first handle in a block.
 *
 */
LPARAM *BlockFetchUserDataPointer(TableSegment *pSegment, UINT uBlock, BOOL fAssertOnError)
{
    // assume NULL until we actually find the data
    LPARAM *pUserData = NULL;

    // get the user data index for this block
    UINT uData = pSegment->rgUserData[uBlock];

    // is there user data for the block?
    if (uData != BLOCK_INVALID)
    {
        // yes - compute the address of the user data
        pUserData = (LPARAM *)(pSegment->rgValue + (uData * HANDLE_HANDLES_PER_BLOCK));
    }
    else if (fAssertOnError)
    {
        // no user data is associated with this block
        //
        // we probably got here for one of the following reasons:
        //  1) an outside caller tried to do a user data operation on an incompatible handle
        //  2) the user data map in the segment is corrupt
        //  3) the global type flags are corrupt
        //
        _ASSERTE(FALSE);
    }

    // return the result
    return pUserData;
}


/*
 * HandleFetchSegmentPointer
 *
 * Computes the segment pointer for a given handle.
 *
 */
__inline TableSegment *HandleFetchSegmentPointer(OBJECTHANDLE handle)
{
    // find the segment for this handle
    TableSegment *pSegment = (TableSegment *)((UINT_PTR)handle & HANDLE_SEGMENT_ALIGN_MASK);

    // sanity
    _ASSERTE(pSegment);

    // return the segment pointer
    return pSegment;
}


/*
 * HandleValidateAndFetchUserDataPointer
 *
 * Gets the user data pointer for the specified handle.
 * ASSERTs and returns NULL if handle is not of the expected type.
 *
 */
LPARAM *HandleValidateAndFetchUserDataPointer(OBJECTHANDLE handle, UINT uTypeExpected)
{
    // get the segment for this handle
    TableSegment *pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    UINT_PTR offset = (UINT_PTR)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    UINT uHandle = (UINT)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    UINT uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // fetch the user data for this block
    LPARAM *pUserData = BlockFetchUserDataPointer(pSegment, uBlock, TRUE);

    // did we get the user data block?
    if (pUserData)
    {
        // yup - adjust the pointer to be handle-specific
        pUserData += (uHandle - (uBlock * HANDLE_HANDLES_PER_BLOCK));

        // validate the block type before returning the pointer
        if (pSegment->rgBlockType[uBlock] != uTypeExpected)
        {
            // type mismatch - caller error
            _ASSERTE(FALSE);

            // don't return a pointer to the caller
            pUserData = NULL;
        }
    }

    // return the result
    return pUserData;
}


/*
 * HandleQuickFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * Less validation is performed.
 *
 */
LPARAM *HandleQuickFetchUserDataPointer(OBJECTHANDLE handle)
{
    // get the segment for this handle
    TableSegment *pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    UINT_PTR offset = (UINT_PTR)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    UINT uHandle = (UINT)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    UINT uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // fetch the user data for this block
    LPARAM *pUserData = BlockFetchUserDataPointer(pSegment, uBlock, TRUE);

    // if we got the user data block then adjust the pointer to be handle-specific
    if (pUserData)
        pUserData += (uHandle - (uBlock * HANDLE_HANDLES_PER_BLOCK));

    // return the result
    return pUserData;
}


/*
 * HandleQuickSetUserData
 *
 * Stores user data with a handle.
 *
 */
void HandleQuickSetUserData(OBJECTHANDLE handle, LPARAM lUserData)
{
    // fetch the user data slot for this handle
    LPARAM *pUserData = HandleQuickFetchUserDataPointer(handle);

    // is there a slot?
    if (pUserData)
    {
        // yes - store the info
        *pUserData = lUserData;
    }
}


/*
 * HandleFetchType
 *
 * Computes the type index for a given handle.
 *
 */
UINT HandleFetchType(OBJECTHANDLE handle)
{
    // get the segment for this handle
    TableSegment *pSegment = HandleFetchSegmentPointer(handle);

    // find the offset of this handle into the segment
    UINT_PTR offset = (UINT_PTR)handle & HANDLE_SEGMENT_CONTENT_MASK;

    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);

    // convert the offset to a handle index
    UINT uHandle = (UINT)((offset - HANDLE_HEADER_SIZE) / HANDLE_SIZE);

    // compute the block this handle resides in
    UINT uBlock = uHandle / HANDLE_HANDLES_PER_BLOCK;

    // return the block's type
    return pSegment->rgBlockType[uBlock];
}
    
/*
 * HandleFetchHandleTable
 *
 * Computes the type index for a given handle.
 *
 */
HandleTable *HandleFetchHandleTable(OBJECTHANDLE handle)
{
    // get the segment for this handle
    TableSegment *pSegment = HandleFetchSegmentPointer(handle);

    // return the table
    return pSegment->pHandleTable;
}

/*
 * SegmentInitialize
 *
 * Initializes a segment.
 *
 */
BOOL SegmentInitialize(TableSegment *pSegment, HandleTable *pTable)
{
    // we want to commit enough for the header PLUS some handles
    DWORD32 dwCommit =
        (HANDLE_HEADER_SIZE + g_SystemInfo.dwPageSize) & (~(g_SystemInfo.dwPageSize - 1));

    // commit the header
    if (!VirtualAlloc(pSegment, dwCommit, MEM_COMMIT, PAGE_READWRITE))
    {
        _ASSERTE(FALSE);
        return FALSE;
    }

    // remember how many blocks we commited
    pSegment->bCommitLine = (BYTE)((dwCommit - HANDLE_HEADER_SIZE) / HANDLE_BYTES_PER_BLOCK);

    // now preinitialize the 0xFF guys
    FillMemory(pSegment->rgGeneration, sizeof(pSegment->rgGeneration), 0xFF);
    FillMemory(pSegment->rgTail,       sizeof(pSegment->rgTail),       BLOCK_INVALID);
    FillMemory(pSegment->rgHint,       sizeof(pSegment->rgHint),       BLOCK_INVALID);
    FillMemory(pSegment->rgFreeMask,   sizeof(pSegment->rgFreeMask),   0xFF);
    FillMemory(pSegment->rgBlockType,  sizeof(pSegment->rgBlockType),  TYPE_INVALID);
    FillMemory(pSegment->rgUserData,   sizeof(pSegment->rgUserData),   BLOCK_INVALID);

    // prelink the free chain
    UINT u = 0;
    while (u < (HANDLE_BLOCKS_PER_SEGMENT - 1))
    {
        UINT next = u + 1;
        pSegment->rgAllocation[u] = next;
        u = next;
    }

    // and terminate the last node
    pSegment->rgAllocation[u] = BLOCK_INVALID;

    // store the back pointer from our new segment to its owning table
    pSegment->pHandleTable = pTable;

    // all done
    return TRUE;
}


/*
 * SegmentFree
 *
 * Frees the specified segment.
 *
 */
void SegmentFree(TableSegment *pSegment)
{
    // free the segment's memory
    VirtualFree(pSegment, 0, MEM_RELEASE);
}


/*
 * SegmentAlloc
 *
 * Allocates a new segment.
 *
 */
TableSegment *SegmentAlloc(HandleTable *pTable)
{
    // allocate the segment's address space
    TableSegment *pSegment =
        (TableSegment *)ReserveAlignedMemory(HANDLE_SEGMENT_SIZE, HANDLE_SEGMENT_SIZE);

    // bail out if we couldn't get any memory
    if (!pSegment)
    {
        _ASSERTE(FALSE);
        return NULL;
    }

    // initialize the header
    if (!SegmentInitialize(pSegment, pTable))
    {
        SegmentFree(pSegment);
        pSegment = NULL;
    }

    // all done
    return pSegment;
}


/*
 * SegmentRemoveFreeBlocks
 *
 * Scans a segment for free blocks of the specified type
 * and moves them to the segment's free list.
 *
 */
void SegmentRemoveFreeBlocks(TableSegment *pSegment, UINT uType, BOOL *pfScavengeLater)
{
    // fetch the tail block for the specified chain
    UINT uPrev = pSegment->rgTail[uType];

    // if it's a terminator then there are no blocks in the chain
    if (uPrev == BLOCK_INVALID)
        return;

    // we may need to clean up user data blocks later
    BOOL fCleanupUserData = FALSE;

    // start iterating with the head block
    UINT uStart = pSegment->rgAllocation[uPrev];
    UINT uBlock = uStart;

    // keep track of how many blocks we removed
    UINT uRemoved = 0;

    // we want to preserve the relative order of any blocks we free
    // this is the best we can do until the free list is resorted
    UINT uFirstFreed = BLOCK_INVALID;
    UINT uLastFreed  = BLOCK_INVALID;

    // loop until we've processed the whole chain
    for (;;)
    {
        // fetch the next block index
        UINT uNext = pSegment->rgAllocation[uBlock];

#ifdef HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS
        // determine whether this block is empty
        if (((PUINT64)pSegment->rgFreeMask)[uBlock] == 0xFFFFFFFFFFFFFFFFUL)
#else
        // assume this block is empty until we know otherwise
        BOOL fEmpty = TRUE;

        // get the first mask for this block
        DWORD32 *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
        DWORD32 *pdwMaskLast = pdwMask              + HANDLE_MASKS_PER_BLOCK;

        // loop through the masks until we've processed them all or we've found handles
        do
        {
            // is this mask empty?
            if (*pdwMask != MASK_EMPTY)
            {
                // nope - this block still has handles in it
                fEmpty = FALSE;
                break;
            }

            // on to the next mask
            pdwMask++;

        } while (pdwMask < pdwMaskLast);

        // is this block empty?
        if (fEmpty)
#endif
        {
            // is this block currently locked?
            if (BlockIsLocked(pSegment, uBlock))
            {
                // block cannot be freed, if we were passed a scavenge flag then set it
                if (pfScavengeLater)
                    *pfScavengeLater = TRUE;
            }
            else
            {
                // safe to free - did it have user data associated?
                UINT uData = pSegment->rgUserData[uBlock];
                if (uData != BLOCK_INVALID)
                {
                    // data blocks are 'empty' so we keep them locked
                    // unlock the block so it can be reclaimed below
                    BlockUnlock(pSegment, uData);

                    // unlink the data block from the handle block
                    pSegment->rgUserData[uBlock] = BLOCK_INVALID;

                    // remember that we need to scavenge the data block chain
                    fCleanupUserData = TRUE;
                }

                // mark the block as free
                pSegment->rgBlockType[uBlock] = TYPE_INVALID;

                // have we freed any other blocks yet?
                if (uFirstFreed == BLOCK_INVALID)
                {
                    // no - this is the first one - remember it as the new head
                    uFirstFreed = uBlock;
                }
                else
                {
                    // yes - link this block to the other ones in order
                    pSegment->rgAllocation[uLastFreed] = (BYTE)uBlock;
                }

                // remember this block for later
                uLastFreed = uBlock;

                // are there other blocks in the chain?
                if (uPrev != uBlock)
                {
                    // yes - unlink this block from the chain
                    pSegment->rgAllocation[uPrev] = (BYTE)uNext;

                    // if we are removing the tail then pick a new tail
                    if (pSegment->rgTail[uType] == uBlock)
                        pSegment->rgTail[uType] = (BYTE)uPrev;

                    // if we are removing the hint then pick a new hint
                    if (pSegment->rgHint[uType] == uBlock)
                        pSegment->rgHint[uType] = (BYTE)uNext;

                    // we removed the current block - reset uBlock to a valid block
                    uBlock = uPrev;

                    // N.B. we'll check if we freed uStart later when it's safe to recover
                }
                else
                {
                    // we're removing last block - sanity check the loop condition
                    _ASSERTE(uNext == uStart);

                    // mark this chain as completely empty
                    pSegment->rgAllocation[uBlock] = BLOCK_INVALID;
                    pSegment->rgTail[uType]        = BLOCK_INVALID;
                    pSegment->rgHint[uType]        = BLOCK_INVALID;
                }

                // update the number of blocks we've removed
                uRemoved++;
            }
        }

        // if we are back at the beginning then it is time to stop
        if (uNext == uStart)
            break;

        // now see if we need to reset our start block
        if (uStart == uLastFreed)
            uStart = uNext;

        // on to the next block
        uPrev = uBlock;
        uBlock = uNext;
    }

    // did we remove any blocks?
    if (uRemoved)
    {
        // yes - link the new blocks into the free list
        pSegment->rgAllocation[uLastFreed] = pSegment->bFreeList;
        pSegment->bFreeList = (BYTE)uFirstFreed;

        // update the free count for this chain
        pSegment->rgFreeCount[uType] -= (uRemoved * HANDLE_HANDLES_PER_BLOCK);

        // mark for a resort - the free list (and soon allocation chains) may be out of order
        pSegment->fResortChains = TRUE;

        // if we removed blocks that had user data then we need to reclaim those too
        if (fCleanupUserData)
            SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);
    }
}


/*
 * SegmentInsertBlockFromFreeListWorker
 *
 * Inserts a block into a block list within a segment.  Blocks are obtained from the
 * segment's free list.  Returns the index of the block inserted, or BLOCK_INVALID
 * if no blocks were avaliable.
 *
 * This routine is the core implementation for SegmentInsertBlockFromFreeList.
 *
 */
UINT SegmentInsertBlockFromFreeListWorker(TableSegment *pSegment, UINT uType, BOOL fUpdateHint)
{
    // fetch the next block from the free list
    UINT uBlock = pSegment->bFreeList;

    // if we got the terminator then there are no more blocks
    if (uBlock != BLOCK_INVALID)
    {
        // are we eating out of the last empty range of blocks?
        if (uBlock >= pSegment->bEmptyLine)
        {
            // get the current commit line
            UINT uCommitLine = pSegment->bCommitLine;

            // if this block is uncommitted then commit some memory now
            if (uBlock >= uCommitLine)
            {
                // figure out where to commit next
                LPVOID pvCommit = pSegment->rgValue + (uCommitLine * HANDLE_HANDLES_PER_BLOCK);

                // we should commit one more page of handles
                DWORD32 dwCommit = g_SystemInfo.dwPageSize;

                // commit the memory
                if (!VirtualAlloc(pvCommit, dwCommit, MEM_COMMIT, PAGE_READWRITE))
                    return BLOCK_INVALID;

                // use the previous commit line as the new decommit line
                pSegment->bDecommitLine = (BYTE)uCommitLine;

                // adjust the commit line by the number of blocks we commited
                pSegment->bCommitLine = (BYTE)(uCommitLine + (dwCommit / HANDLE_BYTES_PER_BLOCK));
            }

            // update our empty line
            pSegment->bEmptyLine = uBlock + 1;
        }

        // unlink our block from the free list
        pSegment->bFreeList = pSegment->rgAllocation[uBlock];

        // link our block into the specified chain
        UINT uOldTail = pSegment->rgTail[uType];
        if (uOldTail == BLOCK_INVALID)
        {
            // first block, set as head and link to itself
            pSegment->rgAllocation[uBlock] = (BYTE)uBlock;

            // there are no other blocks - update the hint anyway
            fUpdateHint = TRUE;
        }
        else
        {
            // not first block - link circularly
            pSegment->rgAllocation[uBlock] = pSegment->rgAllocation[uOldTail];
            pSegment->rgAllocation[uOldTail] = (BYTE)uBlock;
        
            // chain may need resorting depending on what we added
            pSegment->fResortChains = TRUE;
        }

        // mark this block with the type we're using it for
        pSegment->rgBlockType[uBlock] = (BYTE)uType;

        // update the chain tail
        pSegment->rgTail[uType] = (BYTE)uBlock;

        // if we are supposed to update the hint, then point it at the new block
        if (fUpdateHint)
            pSegment->rgHint[uType] = (BYTE)uBlock;

        // increment the chain's free count to reflect the additional block
        pSegment->rgFreeCount[uType] += HANDLE_HANDLES_PER_BLOCK;
    }

    // all done
    return uBlock;
}


/*
 * SegmentInsertBlockFromFreeList
 *
 * Inserts a block into a block list within a segment.  Blocks are obtained from the
 * segment's free list.  Returns the index of the block inserted, or BLOCK_INVALID
 * if no blocks were avaliable.
 *
 * This routine does the work of securing a parallel user data block if required.
 *
 */
UINT SegmentInsertBlockFromFreeList(TableSegment *pSegment, UINT uType, BOOL fUpdateHint)
{
    UINT uBlock, uData = 0;

    // does this block type require user data?
    BOOL fUserData = TypeHasUserData(pSegment->pHandleTable, uType);

    // if we need user data then we need to make sure it can go in the same segment as the handles
    if (fUserData)
    {
        // if we can't also fit the user data in this segment then bail
        uBlock = pSegment->bFreeList;
        if ((uBlock == BLOCK_INVALID) || (pSegment->rgAllocation[uBlock] == BLOCK_INVALID))
            return BLOCK_INVALID;

        // allocate our user data block (we do it in this order so that free order is nicer)
        uData = SegmentInsertBlockFromFreeListWorker(pSegment, HNDTYPE_INTERNAL_DATABLOCK, FALSE);
    }

    // now allocate the requested block
    uBlock = SegmentInsertBlockFromFreeListWorker(pSegment, uType, fUpdateHint);

    // should we have a block for user data too?
    if (fUserData)
    {
        // did we get them both?
        if ((uBlock != BLOCK_INVALID) && (uData != BLOCK_INVALID))
        {
            // link the data block to the requested block
            pSegment->rgUserData[uBlock] = (BYTE)uData;

            // no handles are ever allocated out of a data block
            // lock the block so it won't be reclaimed accidentally
            BlockLock(pSegment, uData);
        }
        else
        {
            // NOTE: We pre-screened that the blocks exist above, so we should only
            //       get here under heavy load when a MEM_COMMIT operation fails.

            // if the type block allocation succeeded then scavenge the type block list
            if (uBlock != BLOCK_INVALID)
                SegmentRemoveFreeBlocks(pSegment, uType, NULL);

            // if the user data allocation succeeded then scavenge the user data list
            if (uData != BLOCK_INVALID)
                SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);

            // make sure we return failure
            uBlock = BLOCK_INVALID;
        }
    }

    // all done
    return uBlock;
}


/*
 * SegmentResortChains
 *
 * Sorts the block chains for optimal scanning order.
 * Sorts the free list to combat fragmentation.
 *
 */
void SegmentResortChains(TableSegment *pSegment)
{
    // clear the sort flag for this segment
    pSegment->fResortChains = FALSE;

    // first, do we need to scavenge any blocks?
    if (pSegment->fNeedsScavenging)
    {
        // clear the scavenge flag
        pSegment->fNeedsScavenging = FALSE;

        // we may need to explicitly scan the user data chain too
        BOOL fCleanupUserData = FALSE;

        // fetch the empty line for this segment
        UINT uLast = pSegment->bEmptyLine;

        // loop over all active blocks, scavenging the empty ones as we go
        for (UINT uBlock = 0; uBlock < uLast; uBlock++)
        {
            // fetch the block type of this block
            UINT uType = pSegment->rgBlockType[uBlock];

            // only process public block types - we handle data blocks separately
            if (uType < HANDLE_MAX_PUBLIC_TYPES)
            {
#ifdef HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS
                // determine whether this block is empty
                if (((PUINT64)pSegment->rgFreeMask)[uBlock] == 0xFFFFFFFFFFFFFFFFUL)
#else
                // assume this block is empty until we know otherwise
                BOOL fEmpty = TRUE;
    
                // get the first mask for this block
                DWORD32 *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
                DWORD32 *pdwMaskLast = pdwMask              + HANDLE_MASKS_PER_BLOCK;

                // loop through the masks until we've processed them all or we've found handles
                do
                {
                    // is this mask empty?
                    if (*pdwMask != MASK_EMPTY)
                    {
                        // nope - this block still has handles in it
                        fEmpty = FALSE;
                        break;
                    }

                    // on to the next mask
                    pdwMask++;

                } while (pdwMask < pdwMaskLast);

                // is this block empty?
                if (fEmpty)
#endif
                {
                    // is the block unlocked?
                    if (!BlockIsLocked(pSegment, uBlock))
                    {
                        // safe to free - did it have user data associated?
                        UINT uData = pSegment->rgUserData[uBlock];
                        if (uData != BLOCK_INVALID)
                        {
                            // data blocks are 'empty' so we keep them locked
                            // unlock the block so it can be reclaimed below
                            BlockUnlock(pSegment, uData);

                            // unlink the data block from the handle block
                            pSegment->rgUserData[uBlock] = BLOCK_INVALID;

                            // remember that we need to scavenge the data block chain
                            fCleanupUserData = TRUE;
                        }

                        // mark the block as free
                        pSegment->rgBlockType[uBlock] = TYPE_INVALID;

                        // fix up the free count for the block's type
                        pSegment->rgFreeCount[uType] -= HANDLE_HANDLES_PER_BLOCK;

                        // N.B. we don't update the list linkages here since they are rebuilt below
                    }
                }
            }
        }

        // if we have to clean up user data then do that now
        if (fCleanupUserData)
            SegmentRemoveFreeBlocks(pSegment, HNDTYPE_INTERNAL_DATABLOCK, NULL);
    }

    // keep some per-chain data
    BYTE rgChainCurr[HANDLE_MAX_INTERNAL_TYPES];
    BYTE rgChainHigh[HANDLE_MAX_INTERNAL_TYPES];
    BYTE bChainFree = BLOCK_INVALID;
    UINT uEmptyLine = BLOCK_INVALID;
    BOOL fContiguousWithFreeList = TRUE;

    // preinit the chain data to no blocks
    for (UINT uType = 0; uType < HANDLE_MAX_INTERNAL_TYPES; uType++)
        rgChainHigh[uType] = rgChainCurr[uType] = BLOCK_INVALID;

    // scan back through the block types
    UINT uBlock = HANDLE_BLOCKS_PER_SEGMENT;
    while (uBlock > 0)
    {
        // decrement the block index
        uBlock--;

        // fetch the type for this block
        uType = pSegment->rgBlockType[uBlock];

        // is this block allocated?
        if (uType != TYPE_INVALID)
        {
            // looks allocated
            fContiguousWithFreeList = FALSE;
             
            // hope the segment's not corrupt :)
            _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

            // remember the first block we see for each type
            if (rgChainHigh[uType] == BLOCK_INVALID)
                rgChainHigh[uType] = uBlock;

            // link this block to the last one we saw of this type
            pSegment->rgAllocation[uBlock] = rgChainCurr[uType];

            // remember this block in type chain
            rgChainCurr[uType] = (BYTE)uBlock;
        }
        else
        {
            // block is free - is it also contiguous with the free list?
            if (fContiguousWithFreeList)
                uEmptyLine = uBlock;

            // link this block to the last one in the free chain
            pSegment->rgAllocation[uBlock] = bChainFree;

            // add this block to the free list
            bChainFree = (BYTE)uBlock;
        }
    }

    // now close the loops and store the tails
    for (uType = 0; uType < HANDLE_MAX_INTERNAL_TYPES; uType++)
    {
        // get the first block in the list
        BYTE bBlock = rgChainCurr[uType];

        // if there is a list then make it circular and save it
        if (bBlock != BLOCK_INVALID)
        {
            // highest block we saw becomes tail
            UINT uTail = rgChainHigh[uType];

            // store tail in segment
            pSegment->rgTail[uType] = (BYTE)uTail;

            // link tail to head
            pSegment->rgAllocation[uTail] = bBlock;
        }
    }

    // store the new free list head
    pSegment->bFreeList = bChainFree;

    // compute the new empty line
    if (uEmptyLine > HANDLE_BLOCKS_PER_SEGMENT)
        uEmptyLine = HANDLE_BLOCKS_PER_SEGMENT;

    // store the updated empty line
    pSegment->bEmptyLine = (BYTE)uEmptyLine;
}


/*
 * SegmentTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment
 *
 */
void SegmentTrimExcessPages(TableSegment *pSegment)
{
    // fetch the empty and decommit lines
    UINT uEmptyLine    = pSegment->bEmptyLine;
    UINT uDecommitLine = pSegment->bDecommitLine;

    // check to see if we can decommit some handles
    // NOTE: we use '<' here to avoid playing ping-pong on page boundaries
    //       this is OK since the zero case is handled elsewhere (segment gets freed)
    if (uEmptyLine < uDecommitLine)
    {
        // derive some useful info about the page size
        DWORD32 dwPageRound = g_SystemInfo.dwPageSize - 1;
        DWORD32 dwPageMask  = ~dwPageRound;

        // compute the address corresponding to the empty line
        size_t dwLo = (size_t)pSegment->rgValue + (uEmptyLine  * HANDLE_BYTES_PER_BLOCK);

        // adjust the empty line address to the start of the nearest whole empty page
        dwLo = (dwLo + dwPageRound) & dwPageMask;

        // compute the address corresponding to the old commit line
        size_t dwHi = (size_t)pSegment->rgValue + ((UINT)pSegment->bCommitLine * HANDLE_BYTES_PER_BLOCK);

        // is there anything to decommit?
        if (dwHi > dwLo)
        {
            // decommit the memory
            VirtualFree((LPVOID)dwLo, dwHi - dwLo, MEM_DECOMMIT);

            // update the commit line
            pSegment->bCommitLine = (BYTE)((dwLo - (size_t)pSegment->rgValue) / HANDLE_BYTES_PER_BLOCK);

            // compute the address for the new decommit line
            size_t dwDecommitAddr = dwLo - g_SystemInfo.dwPageSize;

            // assume a decommit line of zero until we know otheriwse
            uDecommitLine = 0;

            // if the address is within the handle area then compute the line from the address
            if (dwDecommitAddr > (size_t)pSegment->rgValue)
                uDecommitLine = (UINT)((dwDecommitAddr - (size_t)pSegment->rgValue) / HANDLE_BYTES_PER_BLOCK);

            // update the decommit line
            pSegment->bDecommitLine = (BYTE)uDecommitLine;
        }
    }
}


/*
 * BlockAllocHandlesInMask
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified mask of the specified handle block.
 *
 * Returns the number of available handles actually allocated.
 *
 */
UINT BlockAllocHandlesInMask(TableSegment *pSegment, UINT uBlock,
                             DWORD32 *pdwMask, UINT uHandleMaskDisplacement,
                             OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // keep track of how many handles we have left to allocate
    UINT uRemain = uCount;

    // fetch the free mask into a local so we can play with it
    DWORD32 dwFree = *pdwMask;

    // keep track of our displacement within the mask
    UINT uByteDisplacement = 0;

    // examine the mask byte by byte for free handles
    do
    {
        // grab the low byte of the mask
        DWORD32 dwLowByte = (dwFree & MASK_LOBYTE);

        // are there any free handles here?
        if (dwLowByte)
        {
            // remember which handles we've taken
            DWORD32 dwAlloc = 0;

            // loop until we've allocated all the handles we can from here
            do
            {
                // get the index of the next handle
                UINT uIndex = c_rgLowBitIndex[dwLowByte];

                // compute the mask for the handle we chose
                dwAlloc |= (1 << uIndex);

                // remove this handle from the mask byte
                dwLowByte &= ~dwAlloc;

                // compute the index of this handle in the segment
                uIndex += uHandleMaskDisplacement + uByteDisplacement;

                // store the allocated handle in the handle array
                *pHandleBase = (OBJECTHANDLE)(pSegment->rgValue + uIndex);

                // adjust our count and array pointer
                uRemain--;
                pHandleBase++;

            } while (dwLowByte && uRemain);

            // shift the allocation mask into position
            dwAlloc <<= uByteDisplacement;

            // update the mask to account for the handles we allocated
            *pdwMask &= ~dwAlloc;
        }

        // on to the next byte in the mask
        dwFree >>= BITS_PER_BYTE;
        uByteDisplacement += BITS_PER_BYTE;

    } while (uRemain && dwFree);

    // return the number of handles we got
    return (uCount - uRemain);
}


/*
 * BlockAllocHandlesInitial
 *
 * Allocates a specified number of handles from a newly committed (empty) block.
 *
 */
UINT BlockAllocHandlesInitial(TableSegment *pSegment, UINT uType, UINT uBlock,
                              OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // sanity check
    _ASSERTE(uCount);

    // validate the number of handles we were asked to allocate
    if (uCount > HANDLE_HANDLES_PER_BLOCK)
    {
        _ASSERTE(FALSE);
        uCount = HANDLE_HANDLES_PER_BLOCK;
    }

    // keep track of how many handles we have left to mark in masks
    UINT uRemain = uCount;

    // get the first mask for this block
    DWORD32 *pdwMask = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);

    // loop through the masks, zeroing the appropriate free bits
    do
    {
        // this is a brand new block - all masks we encounter should be totally free
        _ASSERTE(*pdwMask == MASK_EMPTY);

        // pick an initial guess at the number to allocate
        UINT uAlloc = uRemain;

        // compute the default mask based on that count
        DWORD32 dwNewMask = (MASK_EMPTY << uAlloc);

        // are we allocating all of them?
        if (uAlloc >= HANDLE_HANDLES_PER_MASK)
        {
            // shift above has unpredictable results in this case
            dwNewMask = MASK_FULL;
            uAlloc = HANDLE_HANDLES_PER_MASK;
        }

        // set the free mask
        *pdwMask = dwNewMask;

        // update our count and mask pointer
        uRemain -= uAlloc;
        pdwMask++;

    } while (uRemain);

    // compute the bounds for allocation so we can copy the handles
    _UNCHECKED_OBJECTREF *pValue = pSegment->rgValue + (uBlock * HANDLE_HANDLES_PER_BLOCK);
    _UNCHECKED_OBJECTREF *pLast  = pValue + uCount;

    // loop through filling in the output array with handles
    do
    {
        // store the next handle in the next array slot
        *pHandleBase = (OBJECTHANDLE)pValue;

        // increment our source and destination
        pValue++;
        pHandleBase++;

    } while (pValue < pLast);

    // return the number of handles we allocated
    return uCount;
}


/*
 * BlockAllocHandles
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified handle block.
 *
 * Returns the number of available handles actually allocated.
 *
 */
UINT BlockAllocHandles(TableSegment *pSegment, UINT uBlock, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // keep track of how many handles we have left to allocate
    UINT uRemain = uCount;

    // set up our loop and limit mask pointers
    DWORD32 *pdwMask     = pSegment->rgFreeMask + (uBlock * HANDLE_MASKS_PER_BLOCK);
    DWORD32 *pdwMaskLast = pdwMask + HANDLE_MASKS_PER_BLOCK;

    // keep track of the handle displacement for the mask we're scanning
    UINT uDisplacement = uBlock * HANDLE_HANDLES_PER_BLOCK;

    // loop through all the masks, allocating handles as we go
    do
    {
        // if this mask indicates free handles then grab them
        if (*pdwMask)
        {
            // allocate as many handles as we need from this mask
            UINT uSatisfied = BlockAllocHandlesInMask(pSegment, uBlock, pdwMask, uDisplacement, pHandleBase, uRemain);

            // adjust our count and array pointer
            uRemain     -= uSatisfied;
            pHandleBase += uSatisfied;
    
            // if there are no remaining slots to be filled then we are done
            if (!uRemain)
                break;
        }

        // on to the next mask
        pdwMask++;
        uDisplacement += HANDLE_HANDLES_PER_MASK;

    } while (pdwMask < pdwMaskLast);

    // return the number of handles we got
    return (uCount - uRemain);
}


/*
 * SegmentAllocHandlesFromTypeChain
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified segment's block chain for the specified type.  This routine
 * ONLY scavenges existing blocks in the type chain.  No new blocks are committed.
 *
 * Returns the number of available handles actually allocated.
 *
 */
UINT SegmentAllocHandlesFromTypeChain(TableSegment *pSegment, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // fetch the number of handles available in this chain
    UINT uAvail = pSegment->rgFreeCount[uType];

    // is the available count greater than the requested count?
    if (uAvail > uCount)
    {
        // yes - all requested handles are available
        uAvail = uCount;
    }
    else
    {
        // no - we can only satisfy some of the request
        uCount = uAvail;
    }

    // did we find that any handles are available?
    if (uAvail)
    {
        // yes - fetch the head of the block chain and set up a loop limit
        UINT uBlock = pSegment->rgHint[uType];
        UINT uLast = uBlock;

        // loop until we have found all handles known to be available
        for (;;)
        {
            // try to allocate handles from the current block
            UINT uSatisfied = BlockAllocHandles(pSegment, uBlock, pHandleBase, uAvail);

            // did we get everything we needed?
            if (uSatisfied == uAvail)
            {
                // yes - update the hint for this type chain and get out
                pSegment->rgHint[uType] = (BYTE)uBlock;
                break;
            }

            // adjust our count and array pointer
            uAvail      -= uSatisfied;
            pHandleBase += uSatisfied;

            // fetch the next block in the type chain
            uBlock = pSegment->rgAllocation[uBlock];

            // are we out of blocks?
            if (uBlock == uLast)
            {
                // free count is corrupt
                _ASSERTE(FALSE);

                // avoid making the problem any worse
                uCount -= uAvail;
                break;
            }
        }

        // update the free count
        pSegment->rgFreeCount[uType] -= uCount;
    }

    // return the number of handles we got
    return uCount;
}


/*
 * SegmentAllocHandlesFromFreeList
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * by committing blocks from the free list to that type's type chain.
 *
 * Returns the number of available handles actually allocated.
 *
 */
UINT SegmentAllocHandlesFromFreeList(TableSegment *pSegment, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // keep track of how many handles we have left to allocate
    UINT uRemain = uCount;

    // loop allocating handles until we are done or we run out of free blocks
    do
    {
        // start off assuming we can allocate all the handles
        UINT uAlloc = uRemain;

        // we can only get a block-full of handles at a time
        if (uAlloc > HANDLE_HANDLES_PER_BLOCK)
            uAlloc = HANDLE_HANDLES_PER_BLOCK;

        // try to get a block from the free list
        UINT uBlock = SegmentInsertBlockFromFreeList(pSegment, uType, (uRemain == uCount));

        // if there are no free blocks left then we are done
        if (uBlock == BLOCK_INVALID)
            break;

        // initialize the block by allocating the required handles into the array
        uAlloc = BlockAllocHandlesInitial(pSegment, uType, uBlock, pHandleBase, uAlloc);

        // adjust our count and array pointer
        uRemain     -= uAlloc;
        pHandleBase += uAlloc;

    } while (uRemain);

    // compute the number of handles we took
    uCount -= uRemain;

    // update the free count by the number of handles we took
    pSegment->rgFreeCount[uType] -= uCount;

    // return the number of handles we got
    return uCount;
}


/*
 * SegmentAllocHandles
 *
 * Attempts to allocate the requested number of handes of the specified type,
 * from the specified segment.
 *
 * Returns the number of available handles actually allocated.
 *
 */
UINT SegmentAllocHandles(TableSegment *pSegment, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // first try to get some handles from the existing type chain
    UINT uSatisfied = SegmentAllocHandlesFromTypeChain(pSegment, uType, pHandleBase, uCount);

    // if there are still slots to be filled then we need to commit more blocks to the type chain
    if (uSatisfied < uCount)
    {
        // adjust our count and array pointer
        uCount      -= uSatisfied;
        pHandleBase += uSatisfied;

        // get remaining handles by committing blocks from the free list
        uSatisfied += SegmentAllocHandlesFromFreeList(pSegment, uType, pHandleBase, uCount);
    }

    // return the number of handles we got
    return uSatisfied;
}


/*
 * TableAllocBulkHandles
 *
 * Attempts to allocate the requested number of handes of the specified type.
 *
 * Returns the number of handles that were actually allocated.  This is always
 * the same as the number of handles requested except in out-of-memory conditions,
 * in which case it is the number of handles that were successfully allocated.
 *
 */
UINT TableAllocBulkHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // keep track of how many handles we have left to allocate
    UINT uRemain = uCount;

    // start with the first segment and loop until we are done
    TableSegment *pSegment = pTable->pSegmentList;
    for (;;)
    {
        // get some handles from the current segment
        UINT uSatisfied = SegmentAllocHandles(pSegment, uType, pHandleBase, uRemain);

        // adjust our count and array pointer
        uRemain     -= uSatisfied;
        pHandleBase += uSatisfied;

        // if there are no remaining slots to be filled then we are done
        if (!uRemain)
            break;

        // fetch the next segment in the chain.
        TableSegment *pNextSegment = pSegment->pNextSegment;

        // if are no more segments then allocate another
        if (!pNextSegment)
        {
            // ok if this fails then we're out of luck
            pNextSegment = SegmentAlloc(pTable);
            if (!pNextSegment)
            {
                // we ran out of memory allocating a new segment.
                // this may not be catastrophic - if there are still some
                // handles in the cache then some allocations may succeed.
                _ASSERTE(FALSE);
                break;
            }

            // set up the correct sequence number for the new segment
            pNextSegment->bSequence = (BYTE)(((UINT)pSegment->bSequence + 1) % 0x100);

            // link the new segment into the list
            pSegment->pNextSegment = pNextSegment;
        }

        // try again with new segment
        pSegment = pNextSegment;
    }

    // return the number of handles we actually got
    return (uCount - uRemain);
}


/*
 * BlockFreeHandlesInMask
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different mask is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
UINT BlockFreeHandlesInMask(TableSegment *pSegment, UINT uBlock, UINT uMask, OBJECTHANDLE *pHandleBase, UINT uCount,
                            LPARAM *pUserData, UINT *puActualFreed, BOOL *pfAllMasksFree)
{
    // keep track of how many handles we have left to free
    UINT uRemain = uCount;

    // if this block has user data, convert the pointer to be mask-relative
    if (pUserData)
        pUserData += (uMask * HANDLE_HANDLES_PER_MASK);

    // convert our mask index to be segment-relative
    uMask += (uBlock * HANDLE_MASKS_PER_BLOCK);

    // compute the handle bounds for our mask
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)(pSegment->rgValue + (uMask * HANDLE_HANDLES_PER_MASK));
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_MASK);

    // keep a local copy of the free mask to update as we free handles
    DWORD32 dwFreeMask = pSegment->rgFreeMask[uMask];

    // keep track of how many bogus frees we are asked to do
    UINT uBogus = 0;

    // loop freeing handles until we encounter one outside our block or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // sanity check - the handle should no longer refer to an object here
        _ASSERTE(*(_UNCHECKED_OBJECTREF *)handle == 0);

        // compute the handle index within the mask
        UINT uHandle = (UINT)(handle - firstHandle);

        // if there is user data then clear the user data for this handle
        if (pUserData)
            pUserData[uHandle] = 0L;

        // compute the mask bit for this handle
        DWORD32 dwFreeBit = (1 << uHandle);

        // the handle should not already be free
        if ((dwFreeMask & dwFreeBit) != 0)
        {
            // SOMEONE'S FREEING A HANDLE THAT ISN'T ALLOCATED
            uBogus++;
            _ASSERTE(FALSE);
        }

        // add this handle to the tally of freed handles
        dwFreeMask |= dwFreeBit;

        // adjust our count and array pointer
        uRemain--;
        pHandleBase++;

    } while (uRemain);

    // update the mask to reflect the handles we changed
    pSegment->rgFreeMask[uMask] = dwFreeMask;

    // if not all handles in this mask are free then tell our caller not to check the block
    if (dwFreeMask != MASK_EMPTY)
        *pfAllMasksFree = FALSE;

    // compute the number of handles we processed from the array
    UINT uFreed = (uCount - uRemain);

    // tell the caller how many handles we actually freed
    *puActualFreed += (uFreed - uBogus);

    // return the number of handles we actually freed
    return uFreed;
}


/*
 * BlockFreeHandles
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different block is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
UINT BlockFreeHandles(TableSegment *pSegment, UINT uBlock, OBJECTHANDLE *pHandleBase, UINT uCount,
                      UINT *puActualFreed, BOOL *pfScanForFreeBlocks)
{
    // keep track of how many handles we have left to free
    UINT uRemain = uCount;

    // fetch the user data for this block, if any
    LPARAM *pBlockUserData = BlockFetchUserDataPointer(pSegment, uBlock, FALSE);

    // compute the handle bounds for our block
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)(pSegment->rgValue + (uBlock * HANDLE_HANDLES_PER_BLOCK));
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_BLOCK);

    // this variable will only stay TRUE if all masks we touch end up in the free state
    BOOL fAllMasksWeTouchedAreFree = TRUE;

    // loop freeing handles until we encounter one outside our block or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // compute the mask that this handle resides in
        UINT uMask = (UINT)((handle - firstHandle) / HANDLE_HANDLES_PER_MASK);

        // free as many handles as this mask owns from the front of the array
        UINT uFreed = BlockFreeHandlesInMask(pSegment, uBlock, uMask, pHandleBase, uRemain,
                                             pBlockUserData, puActualFreed, &fAllMasksWeTouchedAreFree);

        // adjust our count and array pointer
        uRemain     -= uFreed;
        pHandleBase += uFreed;

    } while (uRemain);

    // are all masks we touched free?
    if (fAllMasksWeTouchedAreFree)
    {
        // is the block unlocked?
        if (!BlockIsLocked(pSegment, uBlock))
        {
            // tell the caller it might be a good idea to scan for free blocks
            *pfScanForFreeBlocks = TRUE;
        }
    }

    // return the number of handles we actually freed
    return (uCount - uRemain);
}


/*
 * SegmentFreeHandles
 *
 * Frees some portion of an array of handles of the specified type.
 * The array is scanned forward and handles are freed until a handle
 * from a different segment is encountered.
 *
 * Returns the number of handles that were freed from the front of the array.
 *
 */
UINT SegmentFreeHandles(TableSegment *pSegment, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // keep track of how many handles we have left to free
    UINT uRemain = uCount;

    // compute the handle bounds for our segment
    OBJECTHANDLE firstHandle = (OBJECTHANDLE)pSegment->rgValue;
    OBJECTHANDLE lastHandle  = (OBJECTHANDLE)((_UNCHECKED_OBJECTREF *)firstHandle + HANDLE_HANDLES_PER_SEGMENT);

    // the per-block free routines will set this if there is a chance some blocks went free
    BOOL fScanForFreeBlocks = FALSE;

    // track the number of handles we actually free
    UINT uActualFreed = 0;

    // loop freeing handles until we encounter one outside our segment or there are none left
    do
    {
        // fetch the next handle in the array
        OBJECTHANDLE handle = *pHandleBase;

        // if the handle is outside our segment then we are done
        if ((handle < firstHandle) || (handle >= lastHandle))
            break;

        // compute the block that this handle resides in
        UINT uBlock = (UINT)((handle - firstHandle) / HANDLE_HANDLES_PER_BLOCK);

        // sanity check that this block is the type we expect to be freeing
        _ASSERTE(pSegment->rgBlockType[uBlock] == uType);

        // free as many handles as this block owns from the front of the array
        UINT uFreed = BlockFreeHandles(pSegment, uBlock, pHandleBase, uRemain, &uActualFreed, &fScanForFreeBlocks);

        // adjust our count and array pointer
        uRemain     -= uFreed;
        pHandleBase += uFreed;

    } while (uRemain);

    // compute the number of handles we actually freed
    UINT uFreed = (uCount - uRemain);

    // update the free count
    pSegment->rgFreeCount[uType] += uActualFreed;

    // if we saw blocks that may have gone totally free then do a free scan
    if (fScanForFreeBlocks)
    {
        // assume we no scavenging is required
        BOOL fNeedsScavenging = FALSE;

        // try to remove any free blocks we may have created
        SegmentRemoveFreeBlocks(pSegment, uType, &fNeedsScavenging);

        // did SegmentRemoveFreeBlocks have to skip over any free blocks?
        if (fNeedsScavenging)
        {
            // yup, arrange to scavenge them later
            pSegment->fResortChains    = TRUE;
            pSegment->fNeedsScavenging = TRUE;
        }
    }

    // return the total number of handles we freed
    return uFreed;
}


/*
 * TableFreeBulkPreparedHandles
 *
 * Frees an array of handles of the specified type.
 *
 * This routine is optimized for a sorted array of handles but will accept any order.
 *
 */
void TableFreeBulkPreparedHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount)
{
    // loop until all handles are freed
    do
    {
        // get the segment for the first handle
        TableSegment *pSegment = HandleFetchSegmentPointer(*pHandleBase);

        // sanity
        _ASSERTE(pSegment->pHandleTable == pTable);

        // free as many handles as this segment owns from the front of the array
        UINT uFreed = SegmentFreeHandles(pSegment, uType, pHandleBase, uCount);

        // adjust our count and array pointer
        uCount      -= uFreed;
        pHandleBase += uFreed;

    } while (uCount);
}


/*
 * TableFreeBulkUnpreparedHandlesWorker
 *
 * Frees an array of handles of the specified type by preparing them and calling TableFreeBulkPreparedHandles.
 * Uses the supplied scratch buffer to prepare the handles.
 *
 */
void TableFreeBulkUnpreparedHandlesWorker(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandles, UINT uCount,
                                          OBJECTHANDLE *pScratchBuffer)
{
    // copy the handles into the destination buffer
    CopyMemory(pScratchBuffer, pHandles, uCount * sizeof(OBJECTHANDLE));
 
    // sort them for optimal free order
    QuickSort((UINT_PTR *)pScratchBuffer, 0, uCount - 1, CompareHandlesByFreeOrder);
 
    // make sure the handles are zeroed too
    ZeroHandles(pScratchBuffer, uCount);
 
    // prepare and free these handles
    TableFreeBulkPreparedHandles(pTable, uType, pScratchBuffer, uCount);
}
 

/*
 * TableFreeBulkUnpreparedHandles
 *
 * Frees an array of handles of the specified type by preparing them and calling
 * TableFreeBulkPreparedHandlesWorker one or more times.
 *
 */
void TableFreeBulkUnpreparedHandles(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandles, UINT uCount)
{
    // preparation / free buffer
    OBJECTHANDLE rgStackHandles[HANDLE_HANDLES_PER_BLOCK];
    OBJECTHANDLE *pScratchBuffer  = rgStackHandles;
    HLOCAL       hScratchBuffer   = NULL;
    UINT         uFreeGranularity = ARRAYSIZE(rgStackHandles);
 
    // if there are more handles than we can put on the stack then try to allocate a sorting buffer
    if (uCount > uFreeGranularity)
    {
        // try to allocate a bigger buffer to work in
        hScratchBuffer = LocalAlloc(LMEM_FIXED, uCount * sizeof(OBJECTHANDLE));
 
        // did we get it?
        if (hScratchBuffer)
        {
            // yes - use this buffer to prepare and free the handles
            pScratchBuffer   = (OBJECTHANDLE *)hScratchBuffer;
            uFreeGranularity = uCount;
        }
    }
 
    // loop freeing handles until we have freed them all
    while (uCount)
    {
        // decide how many we can process in this iteration
        if (uFreeGranularity > uCount)
            uFreeGranularity = uCount;
 
        // prepare and free these handles
        TableFreeBulkUnpreparedHandlesWorker(pTable, uType, pHandles, uFreeGranularity, pScratchBuffer);
 
        // adjust our pointers and move on
        uCount   -= uFreeGranularity;
        pHandles += uFreeGranularity;
    }
 
    // if we allocated a sorting buffer then free it now
    if (hScratchBuffer)
        LocalFree(hScratchBuffer);
}

/*--------------------------------------------------------------------------*/


