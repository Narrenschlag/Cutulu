#[compute]
#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 10, local_size_y = 1, local_size_z = 1) in;

// A binding to the buffer we create in our script
layout(set = 0, binding = 0, std430) restrict buffer Parameters {
    float data[];
}
params;

float fade(float t) {
  return t*t*t*(t*(t*6.0 - 15.0) + 10.0);
}

float grad(float p) {
  const float texture_width = 256.0;
	float v = texture2D(iChannel0, vec2(p / texture_width, 0.0)).r;
  return v > 0.5 ? 1.0 : -1.0;
}

float noise(float p) {
  float p0 = floor(p);
  float p1 = p0 + 1.0;
    
  float t = p - p0;
  float fade_t = fade(t);

  float g0 = grad(p0);
  float g1 = grad(p1);
  
  return (1.0-fade_t)*g0*(p - p0) + fade_t*g1*(p - p1);
}

// The code we want to execute in each invocation
void main() {
    // gl_GlobalInvocationID.x uniquely identifies this invocation across all work groups
    float val = params.data[gl_GlobalInvocationID.x];
    params.data[gl_GlobalInvocationID.x] = sqrt(noise(val)); 
}
