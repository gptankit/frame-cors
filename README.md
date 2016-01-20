
<b>Frame</b> is a JSON-based, highly configurable CORS manager for Asp.Net Web API

<h3><b>Installing Frame</b></h3>

Frame is available as a nuget package - <i>Frame.CORS</i>. It supports .NET Framework 4.5 and above (till 4.6.2 as of this writing). You can download the latest version from here - https://www.nuget.org/packages/Frame.Cors/ or search "<b>Frame.CORS</b>" from within Nuget Package Manager.

<h3><b>Using Frame</b></h3>

After the installation is finished, you will find a new file created in <i>App_Data</i> folder of your application - <i>frm-cors-config.json</i>. This is a CORS configuration file, and has a JSON array structure like this -

<pre>
[{
   "controllers": "*",
   "allow-origin": "*",
   "allow-methods": "GET,POST",
   "allow-headers": "*",
   "allow-credentials": false,
   "max-age": 0,
   "expose-headers": "*"
},{
   "controllers": "home",
   "allow-origin": "*",
   "allow-methods": "*",
   "allow-headers": "*",
   "allow-credentials": false,
   "max-age": 0,
   "expose-headers": "*"
}]
</pre>

<b>Note</b>: If this file is not created due to permission issues or similar, please create it manually and insert the above JSON or download the attached file in the repo and keep it in <i>App_Data</i> folder.

Consider each JSON element in the array as a set of CORS config for controllers mentioned in the "controllers" key of the same element. This and other key-value pairs are explained below -

<b>controllers</b>: (string) Specify controller names (without Controller suffix, like product) or * for all controllers. All the below described fields will apply to this set of controllers<br/>
<b>allow-origin</b>: (string) Specify a single origin domain (including scheme, host and port, if applicable, like http://example.com) or * for all origins<br/>
<b>allow-methods</b>: (string) Specify access methods allowed in the request<br/>
<b>allow-headers</b>: (string) Specify access headers allowed in the request<br/>
<b>allow-credentials</b>: (boolean) Specify if credentials are allowed in the request<br/>
<b>max-age</b>:  (long) Specify max-age in seconds that the response can be cached for by the user agent<br/>
<b>expose-headers</b>:  (string) Specify which headers are to be exposed to the client in the response

If let's say, there are three controllers (ctrl1, ctrl2, ctrl3) with ctrl1 and ctrl2 having same CORS set, the JSON would look like (example values) -

<pre>
[{
   "controllers": "*",
   "allow-origin": "*",
   "allow-methods": "GET,POST",
   "allow-headers": "*",
   "allow-credentials": false,
   "max-age": 0,
   "expose-headers": "*"
},{
   "controllers": "ctrl1,ctrl2",
   "allow-origin": "http://example.com",
   "allow-methods": "GET,POST,PUT,DELETE",
   "allow-headers": "Authorization,Content-Type",
   "allow-credentials": true,
   "max-age": 300,
   "expose-headers": "*"
},{
   "controllers": "ctrl3",
   "allow-origin": "http://example.com",
   "allow-methods": "GET,POST",
   "allow-headers": "Cache-Control,Content-Type"
}]
</pre>

After the configuration is final, you simply need to import <i>Frame.CORS</i> namespace reference in <i>App_Start/WebApiConfig.cs</i> file and register <b>Frame</b> with this piece of code -

<pre>config.RegisterFrame([bool runLocalOnly]);</pre>

The <i>runLocalOnly</i> flag is optional (default = <i>false</i>). Set it to true only if the API is idle and is being currently used for testing purposes.

<h3><b>Important Points</b></h3>

There are a few important points to note with respect to using the library -

a) As shown in the file above, you can specify a default set of headers for all controllers using "*" as value and then override them by adding another JSON object for a particular controller. Here, the entire new set overrides the default set. and not individual fields. So, if a field like "expose-headers" is to be removed from response, then delete it from the controller's set.<br/>
b) You can omit the default set completely and go on to set individual JSON config for all or a subset of controllers.<br/>
c) Multiple controllers can be grouped together in a comma-seperated manner and that set will be applicable to all of them.<br/>
d) Duplicate controller sets are not allowed. This means that there cannot be two JSON elements having controllers field with same controller name in them. The library will throw a runtime error in such a scenario.<br/>
e) The library allows you to test the CORS functionality locally with the use of a flag (described below). When this is the case, CORS can be assumed to be turned off to the outer world and no cross origin requests are allowed to the server.

<b>Note</b>: Please make sure that the config file name and path are not modified after installation.
