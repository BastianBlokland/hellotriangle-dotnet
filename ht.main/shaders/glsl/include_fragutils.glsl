//Apply a tangent normal (from a normalmap for example) to a worldNormal
//It derrives the tangent and bitangent vectors from change in position and uv
//this eliminates the need to have tangents in the pipeline. 
//Source: http://www.thetenthplanet.de/archives/1180
vec3 perturbNormal(vec3 tangentNormal, vec3 worldNormal, vec3 worldPos, vec2 uv)
{
    vec3 q1 = dFdx(worldPos);
	vec3 q2 = dFdy(worldPos);
	vec2 st1 = dFdx(uv);
	vec2 st2 = dFdy(uv);

	vec3 n = normalize(worldNormal);
	vec3 t = normalize(q1 * st2.t - q2 * st1.t);
	vec3 b = -normalize(cross(n, t));
	mat3 tbn = mat3(t, b, n);

	return normalize(tbn * tangentNormal);
}

vec3 applyNormalTex(sampler2D normalSampler, vec3 worldNormal, vec3 worldPos, vec2 uv)
{
    //Sample and convert from 0 to 1 to -1 to 1
    vec3 tangentNormal = texture(normalSampler, uv).xyz * 2.0 - 1.0;
    return perturbNormal(tangentNormal, worldNormal, worldPos, uv);
}