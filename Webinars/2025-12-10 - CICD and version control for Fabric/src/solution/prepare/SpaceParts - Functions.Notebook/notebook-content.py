# Fabric notebook source

# METADATA ********************

# META {
# META   "kernel_info": {
# META     "name": "synapse_pyspark"
# META   },
# META   "dependencies": {}
# META }

# MARKDOWN ********************

# <center>
# 
# # **Spaceparts - Functions**
# 
# </center>  
# 
# ### Purpose
# Generic notebook containing common python functions and purpose specific functions.
# The functionality in this notebook can be included in other notebooks using the %run command.
# 
# ```
# %run spaceparts_functions
# ```

# MARKDOWN ********************

# ### Install required libraries and import modules

# CELL ********************

pip install pillow cairosvg --quiet  ### Silently install cairosvg. This library is used for converting SVG files to PNG.

# METADATA ********************

# META {
# META   "language": "python",
# META   "language_group": "synapse_pyspark"
# META }

# CELL ********************

import sempy.fabric as fabric
import cairosvg
import io
import base64
import re
import json
import requests
import xml.etree.ElementTree as ET
import time
from PIL import Image, ImageDraw, ImageFont
from requests.adapters import HTTPAdapter, Retry

# METADATA ********************

# META {
# META   "language": "python",
# META   "language_group": "synapse_pyspark"
# META }

# MARKDOWN ********************

# ### Power BI and Fabric API wrapper functions

# CELL ********************

def invoke_api(url:str, method:str = "GET", payload:object = None, audience:str = "pbi"):
    try:
        session = requests.Session()
        retries = Retry(total=3, backoff_factor=5, status_forcelist=[502, 503, 504])
        adapter = HTTPAdapter(max_retries=retries)
        session.mount('http://', adapter)
        session.mount('https://', adapter)

        access_token = notebookutils.credentials.getToken(audience)
        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        response = session.request(method, url, headers=headers, json=payload, timeout=240)

        if response.status_code == 202:
            operation_id = response.headers.get('x-ms-operation-id')

            if operation_id is not None:
                get_operation_state_url = f"https://api.fabric.microsoft.com/v1/operations/{operation_id}"
                while True:
                    operation_state_response = session.request("get", get_operation_state_url, headers=headers)
                    operation_state = operation_state_response.json()
                    status = operation_state.get("status")
                    if status in ["NotStarted", "Running"]:
                        print(".", end="", flush=True)
                        time.sleep(2)
                    elif status == "Succeeded":
                        get_operation_result_url = f"https://api.fabric.microsoft.com/v1/operations/{operation_id}/result"
                        response = session.request("get", get_operation_result_url, headers=headers)
                        break
                    else:
                        print(f"Failed: {operation_state_response}")
                        break

        response_details = {
            'status_code': response.status_code,
            'response': response.json() if response.content else None,
            'headers': dict(response.headers) 
        }
        return response_details

    except requests.RequestException as ex:
        print(f"Error: {ex}")


### Get cluster URL for use in metadata endpoints (unsupported endpoints)
def get_cluster_url():
    for attempt in range(2):  # initial attempt + 1 retry
        response = invoke_api(url="https://api.powerbi.com/v1.0/myorg/capacities")
        match = re.match(
            r"(https://[^/]+/)",
            response.get("response", {}).get("@odata.context", "")
        )

        if match and "redirect.analysis.windows.net" in match.group(1):
            return match.group(1)

        response = invoke_api(url="https://api.powerbi.com/v1.0/myorg/datasets")
        match = re.match(r"(https://[^/]+/)", response.get("response", {}).get("@odata.context", ""))

        if match and "redirect.analysis.windows.net" in match.group(1):
            return match.group(1)

        # Only wait before retrying
        if attempt == 0:
            time.sleep(1)

    return None


def get_workspace_metadata(workspace_id):
    response = invoke_api(url = f"{CLUSTER_BASE_URL}metadata/folders/{workspace_id}")
    return response.get("response")


def set_workspace_icon(workspace_id, base64_png):
    icon = None
    if base64_png == "default":
        icon = ""

    if icon is not None:
        payload = { "icon": icon }
        try:
            response = invoke_api(url = f"{CLUSTER_BASE_URL}metadata/folders/{workspace_id}", method="PUT", payload = payload)
            return response.get("response")
        except:
            print(f"Could not set icon on workspace id {workspace_id}. Ensure that the user is admin on workspace.")
            return None


def get_workspaces():
    response = invoke_api(url = "https://api.fabric.microsoft.com/v1/workspaces")
    return response.get("response")


def refresh_sqlendpoint(workspace_id, sql_endpoint_id):
    endpoint = f"https://api.fabric.microsoft.com/v1/workspaces/{workspace_id}/sqlEndpoints/{sql_endpoint_id}/refreshMetadata"
    response = invoke_api(url = endpoint, method = "POST", payload = {})
    return response.get("response")


def create_table_shortcuts(
    table_names: list[str],
    source_workspace_id: str,
    source_lakehouse_id: str,
    target_workspace_id: str,
    target_lakehouse_id: str):
    
    endpoint = f"https://api.fabric.microsoft.com/v1/workspaces/{workspace_id}/items/{target_lakehouse_id}/shortcuts/bulkCreate?shortcutConflictPolicy=CreateOrOverwrite"
    shortcut_requests = []

    for table_name in table_names:
        shortcut_requests.append({
            "path": "Tables",
            "name": table_name,
            "target": {
                "oneLake": {
                    "workspaceId": workspace_id,
                    "itemId": source_lakehouse_id,
                    "path": f"Tables/{table_name}"
                }
            }
        })

    payload = {
        "createShortcutRequests": shortcut_requests
    }

    response = invoke_api(url = endpoint, method="POST", payload = payload)

# METADATA ********************

# META {
# META   "language": "python",
# META   "language_group": "synapse_pyspark"
# META }

# MARKDOWN ********************

# ### Generic functions

# CELL ********************

def get_environment():
    current_workspace_name = notebookutils.runtime.context.get("currentWorkspaceName")
    env_name = "dev"

    if "[tst]" in current_workspace_name:
        env_name = "tst"
    elif "prd" in current_workspace_name:
        env_name = "prd"

    return env_name


def copy_tables(source_path, target_path, target_lakehouse):
    try:
        print(f"\033[1mCopying tables to {target_lakehouse}\033[0m...")
        tables = notebookutils.fs.ls(f"{source_path}/Tables")
        for table in tables:
             table_path = table.path
             table_name = table_path.split("/")[-1]
            
             df = spark.read.format("delta").load(table_path)
             target_table_path = f"{target_path}/Tables/{table_name}"
            
             df.write.format("delta").mode("overwrite").save(target_table_path)
             print(f" - \033[1m{table_name}\033[0m copied successfully")
        print(f"\033[1mDone copying tables to {target_lakehouse}!\033[0m...\n")
    except Exception as e:
        print(f"Error copying tables to {target_lakehouse}: {e}")


# METADATA ********************

# META {
# META   "language": "python",
# META   "language_group": "synapse_pyspark"
# META }

# MARKDOWN ********************

# ### Workspace icon functions

# CELL ********************

icon_display_size = "24"
default_icon = f"<img height='{icon_display_size}' src='https://content.powerapps.com/resource/powerbiwfe/images/artifact-colored-icons.663f961f5a92d994a109.svg#c_group_workspace_24' />"

def convert_svg_base64_to_png_base64(base64_svg):
    svg_data = base64.b64decode(base64_svg)
    png_bytes = cairosvg.svg2png(bytestring=svg_data)
    base64_png = base64.b64encode(png_bytes).decode()
    return base64_png


def fill_svg(base64_svg, fill_color):
    try:
        svg_data = base64.b64decode(base64_svg).decode('utf-8')
        modified_svg = re.sub(r'fill="[^"]+"', f'fill="{fill_color}"', svg_data)
        return base64.b64encode(modified_svg.encode('utf-8')).decode('utf-8')
    except:
        print("Failed colorfill of image. Skipping")


def filter_items(data, must_contain, either_contain):
    filtered_items = []
    
    for item in data['value']:
        display_name = item.get('displayName', '').lower()
        if must_contain.lower() not in display_name:
            continue  

        if any(sub.lower() in display_name for sub in either_contain):
            filtered_items.append(item)

    return filtered_items


def display_workspace_icons(workspaces):
    html = "<table width='100%'>"
    html += "<th style='text-align:left'>Workspace name</th><th style='text-align:left'>Workspace ID</th><th style='text-align:left; width:100px'>Old icon</th><th style='text-align:left; width:100px'>New icon</th>"
    for workspace in workspaces:
        html += f"<tr><th style='text-align:left'>{workspace.get('displayName')}</td>"
        html += f"<td style='text-align:left'>{workspace.get('id')}</td>"
        iconUrl = get_workspace_metadata(workspace.get('id')).get('iconUrl')
        existing_icon = f"<img height='{icon_display_size}' src='{CLUSTER_BASE_URL}{iconUrl}'/>" if iconUrl is not None else default_icon
        html += f"<td style='text-align:left'>{existing_icon}</td>"
        new_icon = workspace.get('icon_base64img')
        if workspace.get('icon_base64img') == "":
            new_icon = default_icon
        else:
            new_icon = f"<img height='{icon_display_size}' src='data:image/png;base64,{new_icon}' />" if new_icon is not None else existing_icon
        html += f"<td style='text-align:left'>{new_icon}</td></tr>"
    
    displayHTML(html)   


def get_marcs_fabric_icons():
    url = "https://raw.githubusercontent.com/marclelijveld/Fabric-Icons/main/Draw.io_Fabric-Icons.xml"

    response = requests.get(url)
    response.raise_for_status()

    try:
        mxlibrary_data = json.loads(ET.fromstring(response.text).text)
    except (json.JSONDecodeError, ET.ParseError):
        return {}

    img_dict = {}

    for item in mxlibrary_data:
        title = item.get("title", "No Title")
        xml_item = item.get("xml", "")
        
        try:
            root = ET.fromstring(xml_item)
        except ET.ParseError:
            img_dict[title] = "Invalid XML"
            continue
        
        base64_string = next(
            (match.group(1) for cell in root.findall(".//mxCell")
             if (match := re.search(r"image=data:image/[^,]+,([^;]+)", cell.get("style", "")))),
            "No Base64 found"
        )
        
        img_dict[title] = base64_string

    return img_dict


def add_letter_to_base64_png(base64_png, letter, font_size=20, text_color="black", bold=False):
    image_data = base64.b64decode(base64_png)
    image = Image.open(io.BytesIO(image_data))

    draw = ImageDraw.Draw(image)
    
    try:
        font = ImageFont.truetype("arial.ttf", font_size)
    except IOError:
        font = ImageFont.truetype("DejaVuSans-Bold.ttf", font_size)
        
    padding = 0
    text_bbox = draw.textbbox((0, 0), letter, font=font)  # Get bounding box
    text_width = text_bbox[2] - text_bbox[0]
    text_height = text_bbox[3] - text_bbox[1]
    
    text_x = image.width - text_width - padding
    text_y = padding

    if bold:
        for offset in [(0, 0), (1, 0), (0, 1), (1, 1)]:
            draw.text((text_x + offset[0], text_y + offset[1]), letter, font=font, fill=text_color)
    else:
        draw.text((text_x, text_y), letter, font=font, fill=text_color)

    output_buffer = io.BytesIO()
    image.save(output_buffer, format="PNG")
    new_base64_png = base64.b64encode(output_buffer.getvalue()).decode("utf-8")

    return new_base64_png

# METADATA ********************

# META {
# META   "language": "python",
# META   "language_group": "synapse_pyspark"
# META }
