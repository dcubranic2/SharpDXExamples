using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D9;

namespace Helper
{
    static class Utils
    {
        public static bool IsTextureFormatOk(Device device,Format textureFormat, Format adapterFormat)
        {
            Direct3D direct3D = new Direct3D();
            return direct3D.CheckDeviceFormat(0, DeviceType.Hardware, adapterFormat, Usage.None, ResourceType.Texture, textureFormat);
        }
    }


    //BOOL IsTextureFormatOk(D3DFORMAT TextureFormat, D3DFORMAT AdapterFormat)
    //{
    //    HRESULT hr = pD3D->CheckDeviceFormat(D3DADAPTER_DEFAULT,
    //                                          D3DDEVTYPE_HAL,
    //                                          AdapterFormat,
    //                                          0,
    //                                          D3DRTYPE_TEXTURE,
    //                                          TextureFormat);

    //    return SUCCEEDED(hr);
    //}

    //if(FAILED(m_pD3D->GetAdapterDisplayMode(Device.m_uAdapter , &mode)))
    //return E_FAIL;
}
