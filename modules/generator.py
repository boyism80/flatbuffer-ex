import jinja2
import os

def loadTemplateFiles(root):
    loader = jinja2.FileSystemLoader(root)
    env = jinja2.Environment(loader=loader)

    return {os.path.splitext(x)[0]:env.get_template(x) for x in os.listdir(root) if x.endswith('.txt')}

templates = loadTemplateFiles('templates')


def go(namespace, name, params):
    return templates['model'].render({
        'namespace': namespace,
        'name': name,
        'params': params
    })