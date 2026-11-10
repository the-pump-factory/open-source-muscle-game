#define PI_ 3.14159265359
#define E 2.71828182846

float4 Gamma(float4 x) { pow(x, 1.0 / 2.2); }
float4 DeGamma(float4 x) { pow(x, 1.0 / 2.2); }

//normpdf function gives us a Guassian distribution for each blur iteration; 
//this is equivalent of multiplying by hard #s 0.16,0.15,0.12,0.09, etc. in code above
float normpdf(float x, float sigma, float offset)
{
    float stDevSquared = sigma * sigma;
    float gauss = (1 / sqrt(2 * PI_ * stDevSquared)) * pow(E, -((offset * offset) / (2 * stDevSquared)));
    return gauss;
}

float normpdf(float x, float sigma)
{
    return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
}

void blur_half(in UnityTexture2D _MainTex, in UnitySamplerState _MainTexState, in float2 uv, in float blurAmount, in float sigma, out half4 col) {
    //get our base color...
    col = float4(0, 0, 0, 0);//SAMPLE_TEXTURE2D(_MainTex, _MainTexState, uv);
    //total width/height of our blur "grid":
    const int mSize = 6;
    //this gives the number of times we'll iterate our blur on each side 
    //(up,down,left,right) of our uv coordinate;
    //NOTE that this needs to be a const or you'll get errors about unrolling for loops
    const int iter = (mSize - 1) / 2;
    //run loops to do the equivalent of what's written out line by line above
    //(number of blur iterations can be easily sized up and down this way)
    float total = 0;
    for (int i = -iter; i <= iter; ++i) {
        for (int j = -iter; j <= iter; ++j) {
            float v = normpdf(float(i), sigma, (abs(i) + abs(j)) / 2.0);
            //float v = normpdf(float(i), sigma);
            total += v;
            col += SAMPLE_TEXTURE2D(_MainTex, _MainTexState, float2(uv.x + i * blurAmount, uv.y + j * blurAmount)) * v;
        }
    }
    //return blurred color
    col = col / total;
}

void blur_float(in UnityTexture2D _MainTex, in UnitySamplerState _MainTexState, in float2 uv, in float blurAmount, in float sigma, out float4 col) {
    //get our base color...
    col = float4(0, 0, 0, 0); //SAMPLE_TEXTURE2D(_MainTex, _MainTexState, uv);
    //total width/height of our blur "grid":
    const int mSize = 6;
    //this gives the number of times we'll iterate our blur on each side 
    //(up,down,left,right) of our uv coordinate;
    //NOTE that this needs to be a const or you'll get errors about unrolling for loops
    const int iter = (mSize - 1) / 2;
    //run loops to do the equivalent of what's written out line by line above
    //(number of blur iterations can be easily sized up and down this way)
    float total = 0;
    for (int i = -iter; i <= iter; ++i) {
        for (int j = -iter; j <= iter; ++j) {
            float v = normpdf(float(i), sigma, (abs(i) + abs(j)) / 2.0);
            //float v = normpdf(float(i), sigma);
            total += v;
            col += SAMPLE_TEXTURE2D(_MainTex, _MainTexState, float2(uv.x + i * blurAmount, uv.y + j * blurAmount)) * v;
        }
    }
    //return blurred color
    col = col / total;
}