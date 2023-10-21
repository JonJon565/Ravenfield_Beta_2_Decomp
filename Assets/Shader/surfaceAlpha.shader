Shader "EasyRoads3D/EasyRoads3D Surface Transparant" {
Properties {
 _Color ("Main Color", Color) = (1,1,1,1)
 _MainTex ("Base (RGB) Trans (A)", 2D) = "white" { }
}
Fallback "Transparent/VertexLit"
}