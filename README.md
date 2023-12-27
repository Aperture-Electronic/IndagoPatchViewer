# Indago Patch Viewer

The Indago Patch Viewer is a Avalonia GUI application that can extract signals' value from *Cadence(R) Indago(R) Interactive Verification Enviroment* and show them as a image patch.

## Usage
Follow the following steps can easy to use:
1. Start your *Cadence(R) Indago(R)* software in your simulation database directory (it may created by your Xcelium simulator);
2. In the Indago, open the **Console** page, and switch the type of console from 'indago' to 'Python' in the bottom of the page (beside of the command input textbox);
3. Indago console will show some debug information, you can find your server port in the line like "Indago: Attempting to connect to Indago server on host: localhost, port: xxxx";
4. Start Indago Patch Viewer with dotnet, and put the port number into the **Port** textbox;
5. Click the **Connect** button, wait for serval seconds, then your design hierarchy will show in the **Design Hierarchy** as a tree;
6. Find which design included your signal in the tree, can select the item;
7. Set the patch's width and height;
8. Click the **Refresh** button in **Signals** tab, and it will show the signals whose size matches the **Total Bit-width**;
9. Check the **Enabled** checkbox beside of the **Monitor** tab;
10. Move your debug cursor in the Indago (in wave window or change the debug time) then the image will auto-refresh.
