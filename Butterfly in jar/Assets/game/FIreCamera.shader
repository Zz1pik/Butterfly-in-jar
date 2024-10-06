Shader "Custom/FireShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Float) = 1.0
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
        _ColorTop ("Top Color", Color) = (1, 0.5, 0, 1)  // Оранжевый
        _ColorBottom ("Bottom Color", Color) = (1, 0, 0, 1) // Красный
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Speed;
            float4 _Tiling;
            float4 _ColorTop;
            float4 _ColorBottom;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Анимация огня с помощью шума
                float noise = sin(i.uv.y * 10 + _Time.y * _Speed) * 0.1;
                float2 uvOffset = i.uv * _Tiling.xy + float2(0, noise);
                
                // Получаем текстуру шума
                float noiseValue = tex2D(_MainTex, uvOffset).r;

                // Убедитесь, что noiseValue находится в диапазоне от 0 до 1
                noiseValue = clamp(noiseValue, 0.0, 1.0);

                // Создаем градиент от нижнего к верхнему цвету
                float4 color = lerp(_ColorBottom, _ColorTop, noiseValue);

                // Подключаем прозрачность для создания эффекта огня
                color.a = noiseValue;

                return color;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
