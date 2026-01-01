Shader "Custom/Mask"
{

  SubShader
  {
	 Tags {"Queue" = "Transparent+1"}	 

  Pass
     {
      // ZWrite On // default
		  Blend Zero One 
     }
  }

}
