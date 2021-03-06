// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*
 * Generational GC handle manager.  Internal Implementation Header.
 *
 * Shared defines and declarations for handle table implementation.
 *
 * francish
 */

#include "common.h"
#include "HandleTable.h"

#include "vars.hpp"
#include "crst.h"


/*--------------------------------------------------------------------------*/

//@TODO: find a home for this in a project-level header file
#define BITS_PER_BYTE               (8)
#define ARRAYSIZE(x)                (sizeof(x)/sizeof(x[0]))

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MAJOR TABLE DEFINITIONS THAT CHANGE DEPENDING ON THE WEATHER
 *
 ****************************************************************************/

#ifndef _WIN64

    // Win32 - 64k reserved per segment with 4k as header
    #define HANDLE_SEGMENT_SIZE     (0x10000)   // MUST be a power of 2
    #define HANDLE_HEADER_SIZE      (0x1000)    // SHOULD be <= OS page size

#else

    // Win64 - 128k reserved per segment with 4k as header
    #define HANDLE_SEGMENT_SIZE     (0x20000)   // MUST be a power of 2
    #define HANDLE_HEADER_SIZE      (0x1000)    // SHOULD be <= OS page size

#endif


#ifndef _BIG_ENDIAN

    // little-endian write barrier mask manipulation
    #define GEN_CLUMP_0_MASK        (0x000000FF)
    #define NEXT_CLUMP_IN_MASK(dw)  (dw >> BITS_PER_BYTE)

#else

    // big-endian write barrier mask manipulation
    #define GEN_CLUMP_0_MASK        (0xFF000000)
    #define NEXT_CLUMP_IN_MASK(dw)  (dw << BITS_PER_BYTE)

#endif


// if the above numbers change than these will likely change as well
#define HANDLE_HANDLES_PER_CLUMP    (16)        // segment write-barrier granularity
#define HANDLE_HANDLES_PER_BLOCK    (64)        // segment suballocation granularity
#define HANDLE_OPTIMIZE_FOR_64_HANDLE_BLOCKS    // flag for certain optimizations

// maximum number of internally supported handle types
#define HANDLE_MAX_INTERNAL_TYPES   (8)                             // should be a multiple of 4

// number of types allowed for public callers
#define HANDLE_MAX_PUBLIC_TYPES     (HANDLE_MAX_INTERNAL_TYPES - 1) // reserve one internal type

// internal block types
#define HNDTYPE_INTERNAL_DATABLOCK  (HANDLE_MAX_INTERNAL_TYPES - 1) // reserve last type for data blocks

// max number of generations to support statistics on
#define MAXSTATGEN                  (5)

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MORE DEFINITIONS
 *
 ****************************************************************************/

// fast handle-to-segment mapping
#define HANDLE_SEGMENT_CONTENT_MASK     (HANDLE_SEGMENT_SIZE - 1)
#define HANDLE_SEGMENT_ALIGN_MASK       (~HANDLE_SEGMENT_CONTENT_MASK)

// table layout metrics
#define HANDLE_SIZE                     sizeof(_UNCHECKED_OBJECTREF)
#define HANDLE_HANDLES_PER_SEGMENT      ((HANDLE_SEGMENT_SIZE - HANDLE_HEADER_SIZE) / HANDLE_SIZE)
#define HANDLE_BLOCKS_PER_SEGMENT       (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_BLOCK)
#define HANDLE_CLUMPS_PER_SEGMENT       (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_CLUMP)
#define HANDLE_CLUMPS_PER_BLOCK         (HANDLE_HANDLES_PER_BLOCK / HANDLE_HANDLES_PER_CLUMP)
#define HANDLE_BYTES_PER_BLOCK          (HANDLE_HANDLES_PER_BLOCK * HANDLE_SIZE)
#define HANDLE_HANDLES_PER_MASK         (sizeof(DWORD32) * BITS_PER_BYTE)
#define HANDLE_MASKS_PER_SEGMENT        (HANDLE_HANDLES_PER_SEGMENT / HANDLE_HANDLES_PER_MASK)
#define HANDLE_MASKS_PER_BLOCK          (HANDLE_HANDLES_PER_BLOCK / HANDLE_HANDLES_PER_MASK)
#define HANDLE_CLUMPS_PER_MASK          (HANDLE_HANDLES_PER_MASK / HANDLE_HANDLES_PER_CLUMP)

// cache layout metrics
#define HANDLE_CACHE_TYPE_SIZE          128 // 128 == 63 handles per bank
#define HANDLES_PER_CACHE_BANK          ((HANDLE_CACHE_TYPE_SIZE / 2) - 1)

// cache policy defines
#define REBALANCE_TOLERANCE             (HANDLES_PER_CACHE_BANK / 3)
#define REBALANCE_LOWATER_MARK          (HANDLES_PER_CACHE_BANK - REBALANCE_TOLERANCE)
#define REBALANCE_HIWATER_MARK          (HANDLES_PER_CACHE_BANK + REBALANCE_TOLERANCE)

// bulk alloc policy defines
#define SMALL_ALLOC_COUNT               (HANDLES_PER_CACHE_BANK / 10)

// misc constants
#define MASK_FULL                       (0)
#define MASK_EMPTY                      (0xFFFFFFFF)
#define MASK_LOBYTE                     (0x000000FF)
#define TYPE_INVALID                    ((BYTE)0xFF)
#define BLOCK_INVALID                   ((BYTE)0xFF)

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE LAYOUT STRUCTURES
 *
 ****************************************************************************/

/*
 * we need byte packing for the handle table layout to work
 */
#pragma pack(push)
#pragma pack(1)


/*
 * Table Segment Header
 *
 * Defines the layout for a segment's header data.
 */
struct _TableSegmentHeader
{
    /*
     * Write Barrier Generation Numbers
     *
     * Each slot holds four bytes.  Each byte corresponds to a clump of handles.
     * The value of the byte corresponds to the lowest possible generation that a
     * handle in that clump could point into.
     *
     * WARNING: Although this array is logically organized as a BYTE[], it is sometimes
     *  accessed as DWORD32[] when processing bytes in parallel.  Code which treats the
     *  array as an array of DWORD32s must handle big/little endian issues itself.
     */
    BYTE rgGeneration[HANDLE_BLOCKS_PER_SEGMENT * sizeof(DWORD32) / sizeof(BYTE)];

    /*
     * Block Allocation Chains
     *
     * Each slot indexes the next block in an allocation chain.
     */
    BYTE rgAllocation[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Free Masks
     *
     * Masks - 1 bit for every handle in the segment.
     */
    DWORD32 rgFreeMask[HANDLE_MASKS_PER_SEGMENT];

    /*
     * Block Handle Types
     *
     * Each slot holds the handle type of the associated block.
     */
    BYTE rgBlockType[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block User Data Map
     *
     * Each slot holds the index of a user data block (if any) for the associated block.
     */
    BYTE rgUserData[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Block Lock Count
     *
     * Each slot holds a lock count for its associated block.
     * Locked blocks are not freed, even when empty.
     */
    BYTE rgLocks[HANDLE_BLOCKS_PER_SEGMENT];

    /*
     * Allocation Chain Tails
     *
     * Each slot holds the tail block index for an allocation chain.
     */
    BYTE rgTail[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Allocation Chain Hints
     *
     * Each slot holds a hint block index for an allocation chain.
     */
    BYTE rgHint[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Free Count
     *
     * Each slot holds the number of free handles in an allocation chain.
     */
    UINT rgFreeCount[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * Next Segment
     *
     * Points to the next segment in the chain (if we ran out of space in this one).
     */
    struct TableSegment *pNextSegment;

    /*
     * Handle Table
     *
     * Points to owning handle table for this table segment.
     */
    struct HandleTable *pHandleTable;

    /*
     * Flags
     */
    BYTE fResortChains      : 1;    // allocation chains need sorting
    BYTE fNeedsScavenging   : 1;    // free blocks need scavenging
    BYTE _fUnused           : 6;    // unused

    /*
     * Free List Head
     *
     * Index of the first free block in the segment.
     */
    BYTE bFreeList;

    /*
     * Empty Line
     *
     * Index of the first KNOWN block of the last group of unused blocks in the segment.
     */
    BYTE bEmptyLine;

    /*
     * Commit Line
     *
     * Index of the first uncommited block in the segment.
     */
    BYTE bCommitLine;

    /*
     * Decommit Line
     *
     * Index of the first block in the highest committed page of the segment.
     */
    BYTE bDecommitLine;

    /*
     * Sequence
     *
     * Indicates the segment sequence number.
     */
    BYTE bSequence;
};


/*
 * Table Segment
 *
 * Defines the layout for a handle table segment.
 */
struct TableSegment : public _TableSegmentHeader
{
    /*
     * Filler
     */
    BYTE rgUnused[HANDLE_HEADER_SIZE - sizeof(_TableSegmentHeader)];

    /*
     * Handles
     */
    _UNCHECKED_OBJECTREF rgValue[HANDLE_HANDLES_PER_SEGMENT];
};


/*
 * Handle Type Cache
 *
 * Defines the layout of a per-type handle cache.
 */
struct HandleTypeCache
{
    /*
     * reserve bank
     */
    OBJECTHANDLE rgReserveBank[HANDLES_PER_CACHE_BANK];

    /*
     * index of next available handle slot in the reserve bank
     */
    LONG lReserveIndex;

    /*---------------------------------------------------------------------------------
     * N.B. this structure is split up this way so that when HANDLES_PER_CACHE_BANK is
     * large enough, lReserveIndex and lFreeIndex will reside in different cache lines
     *--------------------------------------------------------------------------------*/

    /*
     * free bank
     */
    OBJECTHANDLE rgFreeBank[HANDLES_PER_CACHE_BANK];

    /*
     * index of next empty slot in the free bank
     */
    LONG lFreeIndex;
};


/*
 * restore default packing
 */
#pragma pack(pop)

/*---------------------------------------------------------------------------*/



/****************************************************************************
 *
 * SCANNING PROTOTYPES
 *
 ****************************************************************************/

/*
 * ScanCallbackInfo
 *
 * Carries parameters for per-segment and per-block scanning callbacks.
 *
 */
struct ScanCallbackInfo
{
    TableSegment  *pCurrentSegment; // segment we are presently scanning, if any
    UINT           uFlags;          // HNDGCF_* flags
    BOOL           fEnumUserData;   // whether user data is being enumerated as well
    HANDLESCANPROC pfnScan;         // per-handle scan callback
    LPARAM         param1;          // callback param 1
    LPARAM         param2;          // callback param 2
    DWORD32        dwAgeMask;       // generation mask for ephemeral GCs

#ifdef _DEBUG
    UINT DEBUG_BlocksScanned;
    UINT DEBUG_BlocksScannedNonTrivially;
    UINT DEBUG_HandleSlotsScanned;
    UINT DEBUG_HandlesActuallyScanned;
#endif
};


/*
 * BLOCKSCANPROC
 *
 * Prototype for callbacks that implement per-block scanning logic.
 *
 */
typedef void (CALLBACK *BLOCKSCANPROC)(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * SEGMENTITERATOR
 *
 * Prototype for callbacks that implement per-segment scanning logic.
 *
 */
typedef TableSegment * (CALLBACK *SEGMENTITERATOR)(HandleTable *pTable, TableSegment *pPrevSegment);


/*
 * TABLESCANPROC
 *
 * Prototype for TableScanHandles and xxxTableScanHandlesAsync.
 *
 */
typedef void (CALLBACK *TABLESCANPROC)(HandleTable *pTable,
                                       const UINT *puType, UINT uTypeCount,
                                       SEGMENTITERATOR pfnSegmentIterator,
                                       BLOCKSCANPROC pfnBlockHandler,
                                       ScanCallbackInfo *pInfo);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * ADDITIONAL TABLE STRUCTURES
 *
 ****************************************************************************/

/*
 * AsyncScanInfo
 *
 * Tracks the state of an async scan for a handle table.
 *
 */
struct AsyncScanInfo
{
    /*
     * Underlying Callback Info
     *
     * Specifies callback info for the underlying block handler.
     */
    struct ScanCallbackInfo *pCallbackInfo;

    /*
     * Underlying Segment Iterator
     *
     * Specifies the segment iterator to be used during async scanning.
     */
    SEGMENTITERATOR   pfnSegmentIterator;

    /*
     * Underlying Block Handler
     *
     * Specifies the block handler to be used during async scanning.
     */
    BLOCKSCANPROC     pfnBlockHandler;

    /*
     * Scan Queue
     *
     * Specifies the nodes to be processed asynchronously.
     */
    struct ScanQNode *pScanQueue;

    /*
     * Queue Tail
     *
     * Specifies the tail node in the queue, or NULL if the queue is empty.
     */
    struct ScanQNode *pQueueTail;
};


/*
 * Handle Table
 *
 * Defines the layout of a handle table object.
 */
#pragma warning(push)
#pragma warning(disable : 4200 )  // zero-sized array
struct HandleTable
{
    /*
     * flags describing handle attributes
     *
     * N.B. this is at offset 0 due to frequent access by cache free codepath
     */
    UINT rgTypeFlags[HANDLE_MAX_INTERNAL_TYPES];

    /*
     * memory for lock for this table
     */
    BYTE _LockInstance[sizeof(Crst)];                       // interlocked ops used here

    /*
     * lock for this table
     */
    Crst *pLock;

    /*
     * number of types this table supports
     */
    UINT uTypeCount;

    /*
     * head of segment list for this table
     */
    TableSegment *pSegmentList;

    /*
     * information on current async scan (if any)
     */
    AsyncScanInfo *pAsyncScanInfo;

    /*
     * per-table user info
     */
    UINT uTableIndex;

    /*
     * per-table AppDomain info
     */
    UINT uADIndex;

    /*
     * one-level per-type 'quick' handle cache
     */
    OBJECTHANDLE rgQuickCache[HANDLE_MAX_INTERNAL_TYPES];   // interlocked ops used here

    /*
     * debug-only statistics
     */
#ifdef _DEBUG
    int     _DEBUG_iMaxGen;
    __int64 _DEBUG_TotalBlocksScanned            [MAXSTATGEN];
    __int64 _DEBUG_TotalBlocksScannedNonTrivially[MAXSTATGEN];
    __int64 _DEBUG_TotalHandleSlotsScanned       [MAXSTATGEN];
    __int64 _DEBUG_TotalHandlesActuallyScanned   [MAXSTATGEN];
#endif

    /*
     * primary per-type handle cache
     */
    HandleTypeCache rgMainCache[0];                         // interlocked ops used here
};
#pragma warning(pop)

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HELPERS
 *
 ****************************************************************************/

/*
 * A 32/64 comparison callback
 *
 * @TODO: move/merge into common util file
 *
 */
typedef int (*PFNCOMPARE)(UINT_PTR p, UINT_PTR q);


/*
 * A 32/64 neutral quicksort
 *
 * @TODO: move/merge into common util file
 *
 */
void QuickSort(UINT_PTR *pData, int left, int right, PFNCOMPARE pfnCompare);


/*
 * CompareHandlesByFreeOrder
 *
 * Returns:
 *  <0 - handle P should be freed before handle Q
 *  =0 - handles are eqivalent for free order purposes
 *  >0 - handle Q should be freed before handle P
 *
 */
int CompareHandlesByFreeOrder(UINT_PTR p, UINT_PTR q);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * CORE TABLE MANAGEMENT
 *
 ****************************************************************************/

/*
 * TypeHasUserData
 *
 * Determines whether a given handle type has user data.
 *
 */
__inline BOOL TypeHasUserData(HandleTable *pTable, UINT uType)
{
    // sanity
    _ASSERTE(uType < HANDLE_MAX_INTERNAL_TYPES);

    // consult the type flags
    return (pTable->rgTypeFlags[uType] & HNDF_EXTRAINFO);
}


/*
 * TableCanFreeSegmentNow
 *
 * Determines if it is OK to free the specified segment at this time.
 *
 */
BOOL TableCanFreeSegmentNow(HandleTable *pTable, TableSegment *pSegment);


/*
 * BlockIsLocked
 *
 * Determines if the lock count for the specified block is currently non-zero.
 *
 */
__inline BOOL BlockIsLocked(TableSegment *pSegment, UINT uBlock)
{
    // sanity
    _ASSERTE(uBlock < HANDLE_BLOCKS_PER_SEGMENT);

    // fetch the lock count and compare it to zero
    return (pSegment->rgLocks[uBlock] != 0);
}


/*
 * BlockLock
 *
 * Increases the lock count for a block.
 *
 */
__inline void BlockLock(TableSegment *pSegment, UINT uBlock)
{
    // fetch the old lock count
    BYTE bLocks = pSegment->rgLocks[uBlock];

    // assert if we are about to trash the count
    _ASSERTE(bLocks < 0xFF);

    // store the incremented lock count
    pSegment->rgLocks[uBlock] = bLocks + 1;
}


/*
 * BlockUnlock
 *
 * Decreases the lock count for a block.
 *
 */
__inline void BlockUnlock(TableSegment *pSegment, UINT uBlock)
{
    // fetch the old lock count
    BYTE bLocks = pSegment->rgLocks[uBlock];

    // assert if we are about to trash the count
    _ASSERTE(bLocks > 0);

    // store the decremented lock count
    pSegment->rgLocks[uBlock] = bLocks - 1;
}


/*
 * BlockFetchUserDataPointer
 *
 * Gets the user data pointer for the first handle in a block.
 *
 */
LPARAM *BlockFetchUserDataPointer(TableSegment *pSegment, UINT uBlock, BOOL fAssertOnError);


/*
 * HandleValidateAndFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * ASSERTs and returns NULL if handle is not of the expected type.
 *
 */
LPARAM *HandleValidateAndFetchUserDataPointer(OBJECTHANDLE handle, UINT uTypeExpected);


/*
 * HandleQuickFetchUserDataPointer
 *
 * Gets the user data pointer for a handle.
 * Less validation is performed.
 *
 */
LPARAM *HandleQuickFetchUserDataPointer(OBJECTHANDLE handle);


/*
 * HandleQuickSetUserData
 *
 * Stores user data with a handle.
 * Less validation is performed.
 *
 */
void HandleQuickSetUserData(OBJECTHANDLE handle, LPARAM lUserData);


/*
 * HandleFetchType
 *
 * Computes the type index for a given handle.
 *
 */
UINT HandleFetchType(OBJECTHANDLE handle);


/*
 * HandleFetchHandleTable
 *
 * Returns the containing handle table of a given handle.
 *
 */
HandleTable *HandleFetchHandleTable(OBJECTHANDLE handle);


/*
 * SegmentAlloc
 *
 * Allocates a new segment.
 *
 */
TableSegment *SegmentAlloc(HandleTable *pTable);


/*
 * SegmentFree
 *
 * Frees the specified segment.
 *
 */
void SegmentFree(TableSegment *pSegment);


/*
 * SegmentRemoveFreeBlocks
 *
 * Removes a block from a block list in a segment.  The block is returned to
 * the segment's free list.
 *
 */
void SegmentRemoveFreeBlocks(TableSegment *pSegment, UINT uType);


/*
 * SegmentResortChains
 *
 * Sorts the block chains for optimal scanning order.
 * Sorts the free list to combat fragmentation.
 *
 */
void SegmentResortChains(TableSegment *pSegment);


/*
 * SegmentTrimExcessPages
 *
 * Checks to see if any pages can be decommitted from the segment
 *
 */
void SegmentTrimExcessPages(TableSegment *pSegment);


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
UINT TableAllocBulkHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeBulkPreparedHandles
 *
 * Frees an array of handles of the specified type.
 *
 * This routine is optimized for a sorted array of handles but will accept any order.
 *
 */
void TableFreeBulkPreparedHandles(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeBulkUnpreparedHandles
 *
 * Frees an array of handles of the specified type by preparing them and calling TableFreeBulkPreparedHandles.
 *
 */
void TableFreeBulkUnpreparedHandles(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandles, UINT uCount);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HANDLE CACHE
 *
 ****************************************************************************/

/*
 * TableAllocSingleHandleFromCache
 *
 * Gets a single handle of the specified type from the handle table by
 * trying to fetch it from the reserve cache for that handle type.  If the
 * reserve cache is empty, this routine calls TableCacheMissOnAlloc.
 *
 */
OBJECTHANDLE TableAllocSingleHandleFromCache(HandleTable *pTable, UINT uType);


/*
 * TableFreeSingleHandleToCache
 *
 * Returns a single handle of the specified type to the handle table
 * by trying to store it in the free cache for that handle type.  If the
 * free cache is full, this routine calls TableCacheMissOnFree.
 *
 */
void TableFreeSingleHandleToCache(HandleTable *pTable, UINT uType, OBJECTHANDLE handle);


/*
 * TableAllocHandlesFromCache
 *
 * Allocates multiple handles of the specified type by repeatedly
 * calling TableAllocSingleHandleFromCache.
 *
 */
UINT TableAllocHandlesFromCache(HandleTable *pTable, UINT uType, OBJECTHANDLE *pHandleBase, UINT uCount);


/*
 * TableFreeHandlesToCache
 *
 * Frees multiple handles of the specified type by repeatedly
 * calling TableFreeSingleHandleToCache.
 *
 */
void TableFreeHandlesToCache(HandleTable *pTable, UINT uType, const OBJECTHANDLE *pHandleBase, UINT uCount);

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * TABLE SCANNING
 *
 ****************************************************************************/

/*
 * TableScanHandles
 *
 * Implements the core handle scanning loop for a table.
 *
 */
void CALLBACK TableScanHandles(HandleTable *pTable,
                               const UINT *puType,
                               UINT uTypeCount,
                               SEGMENTITERATOR pfnSegmentIterator,
                               BLOCKSCANPROC pfnBlockHandler,
                               ScanCallbackInfo *pInfo);


/*
 * xxxTableScanHandlesAsync
 *
 * Implements asynchronous handle scanning for a table.
 *
 */
void CALLBACK xxxTableScanHandlesAsync(HandleTable *pTable,
                                       const UINT *puType,
                                       UINT uTypeCount,
                                       SEGMENTITERATOR pfnSegmentIterator,
                                       BLOCKSCANPROC pfnBlockHandler,
                                       ScanCallbackInfo *pInfo);


/*
 * TypesRequireUserDataScanning
 *
 * Determines whether the set of types listed should get user data during scans
 *
 * if ALL types passed have user data then this function will enable user data support
 * otherwise it will disable user data support
 *
 * IN OTHER WORDS, SCANNING WITH A MIX OF USER-DATA AND NON-USER-DATA TYPES IS NOT SUPPORTED
 *
 */
BOOL TypesRequireUserDataScanning(HandleTable *pTable, const UINT *types, UINT typeCount);


/*
 * BuildAgeMask
 *
 * Builds an age mask to be used when examining/updating the write barrier.
 *
 */
DWORD32 BuildAgeMask(UINT uGen);


/*
 * QuickSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 */
TableSegment * CALLBACK QuickSegmentIterator(HandleTable *pTable, TableSegment *pPrevSegment);


/*
 * StandardSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs some maintenance on the segments,
 * primarily making sure the block chains are sorted so that
 * g0 scans are more likely to operate on contiguous blocks.
 *
 */
TableSegment * CALLBACK StandardSegmentIterator(HandleTable *pTable, TableSegment *pPrevSegment);


/*
 * FullSegmentIterator
 *
 * Returns the next segment to be scanned in a scanning loop.
 *
 * This iterator performs full maintenance on the segments,
 * including freeing those it notices are empty along the way.
 *
 */
TableSegment * CALLBACK FullSegmentIterator(HandleTable *pTable, TableSegment *pPrevSegment);


/*
 * BlockScanBlocksWithoutUserData
 *
 * Calls the specified callback for each handle, optionally aging the corresponding generation clumps.
 * NEVER propagates per-handle user data to the callback.
 *
 */
void CALLBACK BlockScanBlocksWithoutUserData(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksWithUserData
 *
 * Calls the specified callback for each handle, optionally aging the corresponding generation clumps.
 * ALWAYS propagates per-handle user data to the callback.
 *
 */
void CALLBACK BlockScanBlocksWithUserData(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockScanBlocksEphemeral
 *
 * Calls the specified callback for each handle from the specified generation.
 * Propagates per-handle user data to the callback if present.
 *
 */
void CALLBACK BlockScanBlocksEphemeral(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocks
 *
 * Ages all clumps in a range of consecutive blocks.
 *
 */
void CALLBACK BlockAgeBlocks(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockAgeBlocksEphemeral
 *
 * Ages all clumps within the specified generation.
 *
 */
void CALLBACK BlockAgeBlocksEphemeral(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);


/*
 * BlockResetAgeMapForBlocks
 *
 * Clears the age maps for a range of blocks.
 *
 */
void CALLBACK BlockResetAgeMapForBlocks(TableSegment *pSegment, UINT uBlock, UINT uCount, ScanCallbackInfo *pInfo);

/*--------------------------------------------------------------------------*/


