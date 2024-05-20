using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice;
using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;


namespace GpuInterop.D3DDemo;

public class D3DContent
{

    public static ID3D11Buffer CreateMesh(ID3D11Device device)
    {
        // Compile Vertex and Pixel shaders
        var vertexShaderByteCode = Compiler.CompileFromFile("D3DDemo\\MiniCube.fx", "VS", "vs_4_0");
        var vertexShader = device.CreateVertexShader(vertexShaderByteCode.Span);

        var pixelShaderByteCode = Compiler.CompileFromFile("D3DDemo\\MiniCube.fx", "PS", "ps_4_0");
        var pixelShader = device.CreatePixelShader(pixelShaderByteCode.Span);

        var signature = Compiler.GetInputSignatureBlob(vertexShaderByteCode.Span);

        var inputElements = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
        };

        // Layout from VertexShader input signature
        var layout = device.CreateInputLayout(
            inputElements,
            signature);

        var verticesBuffer = new[]
            {
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left
                new Vector4(-1.0f, -1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, 1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(-1.0f, 1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
            };

        // Instantiate Vertex buffer from vertex data
        using var dataStream = DataStream.Create(verticesBuffer, true, false);
        using var vertices = device.CreateBuffer(new BufferDescription((int)dataStream.Length, BindFlags.VertexBuffer), dataStream);

        // Create Constant Buffer
        var constantBuffer = device.CreateBuffer(new BufferDescription(Unsafe.SizeOf<Matrix4x4>(), BindFlags.ConstantBuffer, ResourceUsage.Default, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

        var context = device.ImmediateContext;

        // Prepare All the stages
        context.IASetInputLayout(layout);
        context.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);
        context.IASetVertexBuffer(0, vertices, Unsafe.SizeOf<Vector4>() * 2, 0);
        context.VSSetConstantBuffer(0, constantBuffer);
        context.VSSetShader(vertexShader);
        context.PSSetShader(pixelShader);
        return constantBuffer;
    }
}
