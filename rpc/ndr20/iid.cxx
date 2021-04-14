// Copyright (c) 1993-1999 Microsoft Corporation

typedef struct _IID
{
    unsigned long x;
    unsigned short s1;
    unsigned short s2;
    unsigned char  c[8];
} IID;


extern "C"
{

extern const IID IID_IPSFactoryHook = {0x2f5c0480, 0x6bc0, 0x11cf, {0x89, 0xb9, 0x0, 0xaa, 0x0, 0x57, 0xb1, 0x49}};

extern const IID IID_IDispatch = {0x00020400,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IStream = {0x0000000c,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IUnknown      = {0x00000000,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};
extern const IID IID_IClassFactory = {0x00000001,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IRpcChannelBuffer  = {0xD5F56B60,0x593B,0x101A,{0xB5,0x69,0x08,0x00,0x2B,0x2D,0xBF,0x7A}};
extern const IID IID_IRpcChannelBuffer2 = {0x594f31d0,0x7f19,0x11d0,{0xb1,0x94,0x00,0xa0,0xc9,0x0d,0xc8,0xbf}};
extern const IID IID_IRpcChannelBuffer3 = {0x25B15600,0x0115,0x11d0,{0xBF,0x0D,0x00,0xAA,0x00,0xB8,0xDF,0xD2}};
extern const IID IID_IAsyncRpcChannelBuffer = {0xa5029fb6,0x3c34,0x11d1,{0x9c,0x99,0x00,0xc0,0x4f,0xb9,0x98,0xaa}};
                                     
extern const IID IID_IRpcProxyBuffer = {0xD5F56A34,0x593B,0x101A,{0xB5,0x69,0x08,0x00,0x2B,0x2D,0xBF,0x7A}};
extern const IID IID_IRpcStubBuffer  = {0xD5F56AFC,0x593B,0x101A,{0xB5,0x69,0x08,0x00,0x2B,0x2D,0xBF,0x7A}};

extern const IID IID_IPSFactoryBuffer = {0xD5F569D0,0x593B,0x101A,{0xB5,0x69,0x08,0x00,0x2B,0x2D,0xBF,0x7A}};

extern const IID GUID_NULL = {0x0,0x0,0x0,{0x0,0x0,0x0,0x0,0x0,0x0,0x0,0x0}};

extern const IID IID_ISynchronize  = {0x00000030,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};
extern const IID IID_ICallFactory  = {0x1c733a30,0x2a1c,0x11ce,{0xad,0xe5,0x00,0xaa,0x00,0x44,0x77,0x3d}};

extern const IID IID_ITypeFactory = {0x0000002E,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};
extern const IID IID_ITypeMarshal = {0x0000002D,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IMarshal = {0x00000003,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IRpcHelper  = {0x00000149,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};
extern const IID CLSID_RpcHelper = {0x0000032a,0x0000,0x0000,{0xC0,0x00,0x00,0x00,0x00,0x00,0x00,0x46}};

extern const IID IID_IReleaseMarshalBuffers = {0xeb0cb9e8,0x7996,0x11d2,{0x87,0x2e,0x00,0x00,0xf8,0x08,0x08,0x59}};

extern const IID IID_IRpcSyntaxNegotiate = {0x58a08519,0x24c8,0x4935,{0xb4,0x82,0x3f,0xd8,0x23,0x33,0x3a,0x4f}};

}